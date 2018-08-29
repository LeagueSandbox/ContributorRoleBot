FROM python:3.7.0-alpine

WORKDIR /app

COPY ./bot /app

RUN rm -f ./.env && \
    pip install -U pip && \
    pip install -r requirements.txt

ENTRYPOINT ["python", "main.py"]
