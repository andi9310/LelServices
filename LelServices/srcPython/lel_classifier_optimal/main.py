import functools
import operator
import json
import math

import pika
import pymongo

client = pymongo.MongoClient('localhost', 8007)
collection = client.lel.lel_aggregations
QUEUE_NAME = "lel_to_classify_optimal"
PUBLISH_QUEUE_NAME = "lel_classified_optimal"
BANNED_KEYS = {"Configuration", "_id", "Label"}


def probability_density(num, std_dev, mean):
    fact = std_dev * math.sqrt(2.0 * math.pi)
    expo = (num - mean) * (num - mean) / (2.0 * std_dev * std_dev) if std_dev != 0.0 else None
    return 1.0 if num == mean else 0.0 if std_dev == 0.0 else math.exp(-expo) / fact


def calculate_mean_and_std_dev_for_attribute(attribute_name, class_items):
    mean = sum(i[attribute_name] for i in class_items) / len(class_items)
    return {
        "mean": mean,
        "std_dev": math.sqrt(
            sum(i[attribute_name] * i[attribute_name] for i in class_items) / len(class_items) - mean * mean)
    }


def on_message(channel, method_frame, header_frame, body):
    publish_queue_name = header_frame.headers.get("reply_to", PUBLISH_QUEUE_NAME)
    channel.queue_declare(publish_queue_name)
    item_to_classify = json.loads(body.decode("UTF-8"))
    items = list(collection.find())
    items_by_class = {}
    for item in items:
        config = item["Configuration"]
        if config in items_by_class:
            items_by_class[config].append({k: v for k, v in item.items() if k not in BANNED_KEYS})
        else:
            items_by_class[config] = [{k: v for k, v in item.items() if k not in BANNED_KEYS}]

    parameters_by_class = {
        class_number: {attribute_name: calculate_mean_and_std_dev_for_attribute(attribute_name, class_items) for
                       attribute_name in class_items[0].keys()} for class_number, class_items in items_by_class.items()}

    probabilities_by_class = {}

    global_params = {k: calculate_mean_and_std_dev_for_attribute(k, items) for k in items[0].keys() if
                     k not in BANNED_KEYS}

    for k, v in parameters_by_class.items():
        probability = functools.reduce(operator.mul, (
            probability_density(item_to_classify[k1], v1["std_dev"], v1["mean"]) * (
                len(items_by_class[k]) / len(items)) / probability_density(item_to_classify[k1],
                                                                           global_params[k1]["std_dev"],
                                                                           global_params[k1]["mean"]) for k1, v1 in
            v.items()), 1.0)
        probabilities_by_class[k] = probability
        print(probabilities_by_class[k])

    c = max(probabilities_by_class.keys(), key=(lambda key: probabilities_by_class[key]))
    channel.basic_publish("", publish_queue_name, json.dumps({"classified_class": c}).encode("UTF-8"))

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
