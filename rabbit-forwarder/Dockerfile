FROM node:8

# Install pika
COPY ./metrics-client /metrics-client/
WORKDIR /metrics-client
RUN npm install
ENTRYPOINT npm start
