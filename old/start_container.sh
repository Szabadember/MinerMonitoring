docker run -d\
 --restart=always\
 -p 8090:80\
 -p 3000:3000\
 -p 2003-2004:2003-2004\
 -p 2023-2024:2023-2024\
 -p 8125:8125/udp\
 -p 8126:8126\
 -v $PWD/graphite-conf/carbon.conf:/opt/graphite/conf/carbon.conf \
 -v $PWD/graphite-conf/storage-aggregation.conf:/opt/graphite/conf/storage-aggregation.conf \
 -v $PWD/graphite-conf/storage-schemas.conf:/opt/graphite/conf/storage-schemas.conf \
 -v /tmp/grafana-docker/data:/var/lib/grafana \
 -e "GF_SERVER_ROOT_URL=http://grafana.server.name" \
 -e "GF_INSTALL_PLUGINS=grafana-clock-panel,grafana-piechart-panel,grafana-simple-json-datasource  1.2.3" \
 minerrig1/monitoring:test2

