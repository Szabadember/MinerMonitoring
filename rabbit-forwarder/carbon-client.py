""" Module """
import os
import time
import socket
import struct
import pickle
import pika

CARBON_SERVER = os.getenv('CARBON_SERVER', '127.0.0.1')
CARBON_PICKLE_PORT = os.getenv('CARBON_PICKLE_PORT', 2004)
AMQP_URL = os.getenv('AMQP_URL', 'amqp://minerrig1:Qgjk5234@localhost:5672/%2F')
EXCHANGE_NAME = os.getenv('EXCHANGE_NAME', 'monitoring_exchange')

def setupConsumer(url, exchange_name, callbackfnc):
    """ sets consumer up """
    parameters = pika.URLParameters(url)
    connection = pika.BlockingConnection(parameters)
    channel = connection.channel()

    channel.exchange_declare(exchange=exchange_name, type='topic')
    result = channel.queue_declare(exclusive=True)
    queue_name = result.method.queue

    channel.queue_bind(exchange=exchange_name,
                       queue=queue_name,
                       routing_key="*.#")

    channel.basic_consume(callbackfnc,
                          queue=queue_name,
                          no_ack=True)

    channel.start_consuming()

def sendMetrics(sock, metricpairs):
    """ sends metrics """
    now = int(time.time())
    tuples = ([])
    for key, value in metricpairs.iteritems():
        tuples.append((key, (now, value)))
    package = pickle.dumps(tuples, 1)
    size = struct.pack('!L', len(package))
    sock.sendall(size)
    sock.sendall(package)

def callback(ch, method, properties, body):
    """ callback """
    sock = socket.socket()
    try:
        print "Sending %(key)s : %(value)s to carbon" % {'key': method.routing_key, 'value': body}
        sock.connect((CARBON_SERVER, CARBON_PICKLE_PORT))
        sendMetrics(sock, {method.routing_key: int(body)})
        sock.close()
    except socket.error:
        print "Couldn't connect to %(server)s on port %(port)d, is carbon-cache.py running?" % {'server':CARBON_SERVER, 'port':CARBON_PICKLE_PORT}

def main():
    """Wrap it all up together"""
    while True:
        try:
            setupConsumer(AMQP_URL, EXCHANGE_NAME, callback)
        except Exception as exc:
            print "RabbitMQ setup error: %(error)s" % {'error': exc}
        time.sleep(5)

if __name__ == "__main__":
    main()
