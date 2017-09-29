"""Sends mining stats to Graphite"""
import time
import socket
import json
import pika

EXCHANGE_NAME = "exchange"
AMQP_URL = "amqp://minerrig1:Qgjk5234@szabadember.synology.me:5672/%2F"
CLAYMORE_SERVER = '192.168.30.171'
CLAYMORE_PORT = 3333
CLAYMORE_STATS_MESSAGE = '{"id":0,"jsonrpc":"2.0","method":"miner_getstat1"}'

#Settings for legacy monitoring
MACHINE_NAME = 'minerrig1'
CARBON_SERVER = 'szabadember.synology.me'
CARBON_PORT = 2003

def write_metric(name, value):
    """Sends a metric"""
    message = '%s.%s %d %d\n' % (MACHINE_NAME, name, value, int(time.time()))
    sock = socket.socket()
    sock.connect((CARBON_SERVER, CARBON_PORT))
    sock.sendall(message)
    sock.close()

def write_rabbit_metric(metric_name, message):
    """Sends a metric to RabbitMQ"""
    message = str(message)
    routing_key = '%s.%s' % (MACHINE_NAME, metric_name)
    parameters = pika.URLParameters(AMQP_URL)
    connection = pika.BlockingConnection(parameters)
    channel = connection.channel()

    channel.exchange_declare(exchange=EXCHANGE_NAME,
                             type='topic')
    result = channel.basic_publish(exchange=EXCHANGE_NAME,
                                   routing_key=routing_key,
                                   body=message)
    connection.close()
    return result

def get_stats():
    """Gets Claymore stats"""
    sock = socket.socket()
    sock.connect((CLAYMORE_SERVER, CLAYMORE_PORT))
    sock.sendall(CLAYMORE_STATS_MESSAGE)
    reply = sock.recv(1024)
    sock.close()
    print reply
    return json.loads(reply)

def main():
    """main entry point"""
    stats = get_stats()
    result_array = stats["result"]

    version = result_array[0]
    uptime_minutes = int(result_array[1])
    hashrate_and_shares = result_array[2].split(';')
    hashrate_per_gpu = result_array[3].split(';')
    temp_fan = result_array[6].split(';')
    pool_address = result_array[7]

    total_hashrate = int(hashrate_and_shares[0])
    total_shares = int(hashrate_and_shares[1])
    total_rejected_shares = int(hashrate_and_shares[2])

    print "Version: %s" % (version)
    print "Uptime: %d" % (uptime_minutes)
    print "Total hashrate: %d" % (total_hashrate)
    print "Total shares: %d" % (total_shares)
    print "Rejected shares: %d" % (total_rejected_shares)
    print "Pool address: %s" % (pool_address)

    write_metric("uptime", uptime_minutes)
    write_metric("hashrate", total_hashrate)
    write_metric("shares.valid", total_shares)
    write_metric("shares.rejected", total_rejected_shares)

    write_rabbit_metric("uptime", uptime_minutes)
    write_rabbit_metric("hashrate", total_hashrate)
    write_rabbit_metric("shares.valid", total_shares)
    write_rabbit_metric("shares.rejected", total_rejected_shares)

    for i in range(len(hashrate_per_gpu)):
        gpu_num = "gpu%d" % (i)
        gpu_hashrate_key = "%s.hashrate" % (gpu_num)
        gpu_hashrate = int(hashrate_per_gpu[i])
        gpu_temp_key = "%s.temperature" % (gpu_num)
        gpu_temp = int(temp_fan[2*i])
        gpu_fan_key = "%s.fanspeed" % (gpu_num)
        gpu_fan = int(temp_fan[(2*i)+1])
        print "GPU %d hashrate: %d" % (i, gpu_hashrate)
        print "GPU %d temperature: %d" % (i, gpu_temp)
        print "GPU %d fan speed: %d" % (i, gpu_fan)
        print ""
        write_rabbit_metric(gpu_hashrate_key, gpu_hashrate)
        write_rabbit_metric(gpu_temp_key, gpu_temp)
        write_rabbit_metric(gpu_fan_key, gpu_fan)
        write_metric(gpu_hashrate_key, gpu_hashrate)
        write_metric(gpu_temp_key, gpu_temp)
        write_metric(gpu_fan_key, gpu_fan)

main()
