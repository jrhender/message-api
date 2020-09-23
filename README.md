## Description:
A simple API that allows messages to be submitted and retrieved. 

Endpoint is hosted at `https://message-api.azurewebsites.net/message`

If running locally, endpoint is at `https://localhost:5001/message`

Note:
- That DB is currently in-memory so message only persist while process is running
- Need to accept self-signed certificates if running locally

Sample json body of message that can be created via `POST`:
```
{
    "content": "test message"
}
```

## To run:
run tests: `dotnet test`

run API: `dotnet run -p ./MessageAPI`

## Tradeoffs:
- Database is in-memory and uses a single connection
- API isn't versioned
- No pagination or total-count
- Should probably use DTO in the event that domain object gets more complex
- No logging
- No health endpoint