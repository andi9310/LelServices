import json
import math

import pika
import pymongo

client = pymongo.MongoClient('localhost', 8007)
collection = client.lel.lel_aggregations
QUEUE_NAME = "lel_to_classify_optimal"
PUBLISH_QUEUE_NAME = "lel_classified_optimal"


def probability_density(num, std_dev, mean):
    fact = std_dev * math.sqrt(2.0 * math.pi)
    expo = (num - mean) * (num - mean) / (2.0 * std_dev * std_dev) if std_dev != 0.0 else None
    return 1.0 if num == mean else 0.0 if std_dev == 0.0 else math.exp(-expo) / fact


def on_message(channel, method_frame, header_frame, body):
    item_to_classify = json.loads(body.decode("UTF-8"))
    items = list(collection.find())
    items_by_class = {}
    for item in items:
        config = item["Configuration"]
        if config in items_by_class:
            items_by_class[config].append({k: v for k, v in item.items() if k not in {"Configuration", "_id", "Label"}})
        else:
            items_by_class[config] = [{k: v for k, v in item.items() if k not in {"Configuration", "_id", "Label"}}]

    parameters_by_class = {}
    for k, v in items_by_class.items():
        parameters_for_class = {}
        for k2 in v[0].keys():
            parameters_for_class[k2] = {}
            s = 0
            s2 = 0
            for i in v:
                s += i[k2]
                s2 += i[k2] * i[k2]
            parameters_for_class[k2]['mean'] = s / len(v)
            parameters_for_class[k2]['std_dev'] = math.sqrt(
                s2 / len(v) - parameters_for_class[k2]['mean'] * parameters_for_class[k2]['mean'])

        parameters_by_class[k] = parameters_for_class

    probabilities_by_class = {}
    for k, v in parameters_by_class.items():
        probability = 1.0
        for k1, v1 in v.items():
            probability *= probability_density(item_to_classify[k1], v1["std_dev"], v1["mean"])
        probabilities_by_class[k] = probability

    c = max(probabilities_by_class.keys(), key=(lambda key: probabilities_by_class[key]))
    channel.basic_publish("", PUBLISH_QUEUE_NAME, json.dumps({"classified_class": c}).encode("UTF-8"))

    channel.basic_ack(delivery_tag=method_frame.delivery_tag)


def main():
    connection = pika.BlockingConnection(pika.URLParameters("amqp://guest:guest@localhost:8006/%2F"))
    channel = connection.channel()
    channel.queue_declare(QUEUE_NAME)
    channel.queue_declare(PUBLISH_QUEUE_NAME)
    channel.basic_consume(on_message, QUEUE_NAME)
    try:
        channel.start_consuming()
    except KeyboardInterrupt:
        channel.stop_consuming()
    connection.close()


if __name__ == "__main__":
    main()
