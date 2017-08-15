FROM python:2

# Install pika
RUN pip install pika

COPY ./carbon-client.py /carbon-client.py

ENTRYPOINT python /carbon-client.py
