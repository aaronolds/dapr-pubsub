## RUN:

dapr run --log-level debug --app-id routing --components-path .\components\ --app-port 5000 dotnet run

## REST:

curl -X POST http://127.0.0.1:5000/deposit -H "Content-Type: application/json" -d '{ \"id\": \"17\", \"amount\": 12 }'

curl -X POST http://127.0.0.1:5000/withdraw -H "Content-Type: application/json" -d '{ \"id\": \"17\", \"amount\": 10 }'

curl -X GET http://127.0.0.1:5000/17 -H "Content-Type: application/json"

## PUB/SUB

dapr publish --pubsub messagebus --publish-app-id routing  -t withdraw -d '{\"id\": \"17\", \"amount\": 15 }'

dapr publish --pubsub messagebus --publish-app-id routing  -t withdraw -f .\data.json

dapr publish --pubsub messagebus --publish-app-id routing  -t deposit -d '{\"id\": \"17\", \"amount\": 15 }'

dapr publish --pubsub messagebus --publish-app-id routing  -t deposit -f .\data.json


curl -X GET http://127.0.0.1:5000/17 -H "Content-Type: application/json"
