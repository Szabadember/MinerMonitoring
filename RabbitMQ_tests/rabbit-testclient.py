import pika
import random

parameters = pika.URLParameters('amqp://minerrig1:Qgjk5234@localhost:5672/%2F')

connection = pika.BlockingConnection(parameters)

channel = connection.channel()
channel.queue_declare(queue='minerrig1')

channel.basic_publish('',
                      'minerrig1',
                      str(random.randint(1, 10)),
                      pika.BasicProperties(content_type='text/plain',
                                           delivery_mode=1))

connection.close()