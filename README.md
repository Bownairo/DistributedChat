# DistributedChat
A distributed chat service in C#


Run the proxy before anything else, and then run servers
using the `run.sh` with an argument of how many servers to
start, or start them manually with `dotnet run -p Server --server.urls "http://localhost:[free port]" --myAddress "localhost:[different free port]"`
