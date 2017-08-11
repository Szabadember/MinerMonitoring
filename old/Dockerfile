FROM graphiteapp/graphite-statsd:latest

# Install Grafana

RUN apt-get -y update \
  && apt-get -y upgrade \
  && apt-get -y install wget adduser libfontconfig curl ca-certificates \
  && wget https://s3-us-west-2.amazonaws.com/grafana-releases/release/grafana_4.4.3_amd64.deb \
  && dpkg -i grafana_4.4.3_amd64.deb \
  && curl -L https://github.com/tianon/gosu/releases/download/1.7/gosu-amd64 > /usr/sbin/gosu \
  && chmod +x /usr/sbin/gosu

VOLUME ["/var/lib/grafana", "/var/log/grafana", "/etc/grafana"]

EXPOSE 3000

COPY ./run_grafana.sh /run.sh

# Install RabbitMQ

RUN apt-get -y update \
  && apt-get -y upgrade \
  && apt-get -y install rabbitmq-server

ENTRYPOINT ["/run.sh"]
