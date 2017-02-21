import collections
import functools
import json
import math

import pika
import pymongo

GaussianParams = collections.namedtuple("GaussianParams", ["mean", "std_dev"])

client = pymongo.MongoClient('localhost', 8007)
collection = client.lel.lel_aggregations
QUEUE_NAME = "lel_to_classify_optimal"
PUBLISH_QUEUE_NAME = "lel_classified_optimal"
BANNED_KEYS = {"Configuration", "_id", "Label"}


def probability_density(num, gaussian_params):
    fact = gaussian_params.std_dev * math.sqrt(2.0 * math.pi)
    expo = gaussian_params.std_dev != 0.0 \
        and (num - gaussian_params.mean) * (num - gaussian_params.mean) / \
            (2.0 * gaussian_params.std_dev * gaussian_params.std_dev)\
        or None

    return \
        (gaussian_params.std_dev != 0.0
            and math.exp(-expo) / fact)\
        or (num == gaussian_params.mean
            and 1.0)\
        or 0.0


def calculate_mean_and_std_dev_for_attribute(attribute_name, items):
    mean = functools.reduce(lambda x, y: x + y, map(lambda item: item[attribute_name], items)) / len(items)
    return GaussianParams(mean, math.sqrt(
        functools.reduce(
            lambda x, y: x + y,
            map(lambda item: item[attribute_name] * item[attribute_name], items)
        ) / len(items) - mean * mean
    ))


def on_message(channel, method_frame, header_frame, body):
    publish_queue_name = header_frame.headers.get("reply_to", PUBLISH_QUEUE_NAME)
    channel.queue_declare(publish_queue_name)
    item_to_classify = json.loads(body.decode("UTF-8"))
    items = list(collection.find())

    possible_attributes = set(filter(lambda x: x not in BANNED_KEYS, items[0].keys()))

    items_by_class = collections.defaultdict(list)

    def append_to_class(item):
        items_by_class[item["Configuration"]].append(
            dict(map(lambda attribute: (attribute, item[attribute]), possible_attributes))
        )

    list(map(append_to_class, items))

    parameters_by_class = dict(
        map(lambda class_number_and_items: (
            class_number_and_items[0], dict(
                map(lambda attribute_name: (
                    attribute_name, calculate_mean_and_std_dev_for_attribute(
                        attribute_name, class_number_and_items[1])
                ), possible_attributes)
            )
        ), items_by_class.items())
    )

    global_params = dict(map(
        lambda attribute: (attribute, calculate_mean_and_std_dev_for_attribute(attribute, items)), possible_attributes
    ))

    probabilities_by_class = dict(
        map(lambda class_number_and_parameters: (
            class_number_and_parameters[0], functools.reduce(lambda x, y: x * y, map(
                lambda attribute_and_value: probability_density(
                    item_to_classify[attribute_and_value[0]], attribute_and_value[1]
                ) * (len(items_by_class[class_number_and_parameters[0]]) / len(items)) / probability_density(
                    item_to_classify[attribute_and_value[0]], global_params[attribute_and_value[0]]
                ), class_number_and_parameters[1].items()), 1.0)
        ), parameters_by_class.items())
    )

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
