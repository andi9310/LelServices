import collections
import functools
import json
import math
import operator
from typing import Sequence, Mapping

import pika
import pymongo


class GaussianParams:
    def __init__(self, mean: float, std_dev: float):
        self.mean = mean
        self.std_dev = std_dev

    def probability_density(self, num: float):
        fact = self.std_dev * math.sqrt(2.0 * math.pi)
        expo = (num - self.mean) * (num - self.mean) / (
            2.0 * self.std_dev * self.std_dev
        ) if self.std_dev != 0.0 else None
        return 1.0 if num == self.mean else 0.0 if self.std_dev == 0.0 else math.exp(-expo) / fact


client = pymongo.MongoClient('localhost', 8007)
collection = client.lel.lel_aggregations
QUEUE_NAME = "lel_to_classify_optimal"
PUBLISH_QUEUE_NAME = "lel_classified_optimal"
BANNED_KEYS = {"Configuration", "_id", "Label"}


def calculate_mean_and_std_dev_for_attribute(attribute_name: str, items: Sequence[Mapping[str, int]]):
    mean = sum(item[attribute_name] for item in items) / len(items)
    return GaussianParams(mean, math.sqrt(
        sum(item[attribute_name] * item[attribute_name] for item in items) / len(items) - mean * mean
    ))


def on_message(channel: pika.adapters.blocking_connection.BlockingChannel, method_frame: pika.spec.Basic.Deliver,
               header_frame: pika.spec.BasicProperties, body: bytes):

    publish_queue_name = header_frame.headers.get("reply_to", PUBLISH_QUEUE_NAME)
    channel.queue_declare(publish_queue_name)
    item_to_classify = json.loads(body.decode("UTF-8"))
    items = list(collection.find())

    possible_attributes = {attribute for attribute in items[0].keys() if attribute not in BANNED_KEYS}

    items_by_class = collections.defaultdict(list)
    for item in items:
        items_by_class[item["Configuration"]].append({attribute: item[attribute] for attribute in possible_attributes})

    parameters_by_class = {
        class_number: {
            attribute_name: calculate_mean_and_std_dev_for_attribute(attribute_name, class_items)
            for attribute_name in possible_attributes
        } for class_number, class_items in items_by_class.items()
    }

    global_params = {
        attribute: calculate_mean_and_std_dev_for_attribute(attribute, items)
        for attribute in possible_attributes
    }

    probabilities_by_class = {
        class_number: functools.reduce(
            operator.mul, (
                value.probability_density(item_to_classify[attribute]) *
                (len(items_by_class[class_number]) / len(items)) /
                global_params[attribute].probability_density(item_to_classify[attribute])
                for attribute, value in class_parameters.items()
            ), 1.0
        ) for class_number, class_parameters in parameters_by_class.items()
    }

    best_class = max(probabilities_by_class.keys(), key=(lambda key: probabilities_by_class[key]))
    channel.basic_publish("", publish_queue_name, json.dumps({"classified_class": best_class}).encode("UTF-8"))

    channel.basic_ack(delivery_tag=method_frame.delivery_tag)


def main():
    connection = pika.BlockingConnection(pika.URLParameters("amqp://guest:guest@localhost:8006/%2F"))
    channel = connection.channel()
    channel.queue_declare(QUEUE_NAME)
    channel.basic_consume(on_message, QUEUE_NAME)
    try:
        channel.start_consuming()
    except KeyboardInterrupt:
        channel.stop_consuming()
    connection.close()


if __name__ == "__main__":
    main()
