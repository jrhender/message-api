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