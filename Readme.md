#doc-stack-app-api

This project is a subset of https://github.com/schwamster/docStack

This service is the Backend Api of [doc-stack-app](https://github.com/schwamster/doc-stack-app). This follows the "Backend for Frontend" pattern - see [BFF](http://samnewman.io/patterns/architectural/bff/).
It provides the UI with the ability to upload and retrieve documents.

## Status

[![CircleCI](https://circleci.com/gh/schwamster/doc-stack-app-api.svg?style=shield&circle-token)](https://circleci.com/gh/schwamster/doc-stack-app-api)

[![Docker Automated buil](https://img.shields.io/docker/automated/jrottenberg/ffmpeg.svg)](https://hub.docker.com/r/schwamster/doc-stack-app-api/)

## Getting started

The easiest way of getting started is to follow the Getting started Guide of [docStac](https://github.com/schwamster/docStack).
The service requires a running instance of [doc-identity](https://github.com/schwamster/doc-identity), [doc-store](https://github.com/schwamster/doc-store), Redis and RethinkDB. 

TODO: create a docker-compose.yml for the prequisite services.

Update the appsettings.json file accordingly (Depending on where the other services run)

Once the service is running you can navigate to http://localhost:<port>/swagger to explore the Api

## Api Endpoints

Please refer to the swagger document the service provides: http://localhost:<port>/swagger
