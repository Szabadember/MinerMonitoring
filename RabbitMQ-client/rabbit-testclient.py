#!/usr/bin/env python
import pika
import sys

EXCHANGE_NAME = "exchange"

parameters = pika.URLParameters('amqp://minerrig1:Qgjk5234@localhost:5672/%2F')
connection = pika.BlockingConnection(parameters)
channel = connection.channel()

channel.exchange_declare(exchange=EXCHANGE_NAME,
                         type='topic')

routing_key = sys.argv[1] if len(sys.argv) > 2 else 'anonymous.info'
message = ' '.join(sys.argv[2:]) or 'Hello World!'
channel.basic_publish(exchange=EXCHANGE_NAME,
                      routing_key=routing_key,
                      body=message)
print(" [x] Sent %r:%r" % (routing_key, message))
connection.close()