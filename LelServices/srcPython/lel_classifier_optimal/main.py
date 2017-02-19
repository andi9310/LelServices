import collections
import functools
import json
import math
import operator
from typing import NamedTuple

import pika
import pymongo


class GaussianParams(NamedTuple):
    mean: float
    std_dev: float


client = pymongo.MongoClient('localhost', 8007)
collection = client.lel.lel_aggregations
QUEUE_NAME = "lel_to_classify_optimal"
PUBLISH_QUEUE_NAME = "lel_classified_optimal"
BANNED_KEYS = {"Configuration", "_id", "Label"}


def probability_density(num: float, gaussian_params: GaussianParams):
    fact = gaussian_params.std_dev * math.sqrt(2.0 * math.pi)
    expo = (num - gaussian_params.mean) * (num - gaussian_params.mean) / (
        2.0 * gaussian_params.std_dev * gaussian_params.std_dev
    ) if gaussian_params.std_dev != 0.0 else None
    return 1.0 if num == gaussian_params.mean else 0.0 if gaussian_params.std_dev == 0.0 else math.exp(-expo) / fact


def calculate_mean_and_std_dev_for_attribute(attribute_name, class_items):
    mean = sum(item[attribute_name] for item in class_items) / len(class_items)
    return GaussianParams(mean, math.sqrt(
        sum(item[attribute_name] * item[attribute_name] for item in class_items) / len(class_items) - mean * mean
    ))


def on_message(channel, method_frame, header_frame, body):
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
                probability_density(item_to_classify[attribute], value) *
                (len(items_by_class[class_number]) / len(items)) /
                probability_density(item_to_classify[attribute], global_params[attribute])
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
