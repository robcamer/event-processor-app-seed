#!/bin/bash
readonly DOCKER_CONTAINER='eventprocessor'

wait_until_container_healthy() {
  until
    [ "$(docker inspect --format "{{.State.Health.Status}}" "$DOCKER_CONTAINER")" = 'healthy' ]
  do
    # Log container status
    local container_status
    container_status="$(docker inspect --format "{{.State.Health.Status}}" "$DOCKER_CONTAINER")"

    echo "Waiting for container $DOCKER_CONTAINER to be 'healthy', current status is '$container_status'. Sleeping for five seconds..."
    sleep 5
  done
}