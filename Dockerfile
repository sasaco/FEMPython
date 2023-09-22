ARG FUNCTION_DIR="/function"

FROM registry.gitlab.com/frontistr-commons/frontistr/fistr1:v5.1

# Install aws-lambda-cpp build dependencies
RUN apt-get update && \
    apt-get install -y \
    python3 \
    python3-pip

# RUN apt-get update && \
#   apt-get install -y \
#   g++ \
#   make \
#   cmake \
#   unzip \
#   libcurl4-openssl-dev

# Include global arg in this stage of the build
ARG FUNCTION_DIR
# Create function directory
RUN mkdir -p ${FUNCTION_DIR}

# Copy function code
COPY app/* ${FUNCTION_DIR}

# # Install the runtime interface client
RUN pip install \
        --target ${FUNCTION_DIR} \
        awslambdaric

# Multi-stage build: grab a fresh copy of the base image
# FROM python:buster

# Include global arg in this stage of the build
ARG FUNCTION_DIR
# Set working directory to function root directory
WORKDIR ${FUNCTION_DIR}

# Copy in the build image dependencies
# COPY --from=build-image ${FUNCTION_DIR} ${FUNCTION_DIR}

ENTRYPOINT [ "/usr/local/bin/python", "-m", "awslambdaric" ]
CMD [ "app.handler" ]