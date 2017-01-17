docker-compose -f docker-compose-build.yml up
docker build -t doc-stack-app-api ./publish/web
docker run --name api -d -p 3002:80 doc-stack-app-api