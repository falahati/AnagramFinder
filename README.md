# AnagramFinder
This is a NetStandard C# answer to the TrustPilot's Rabbit Hole Challenge

Test system is a i7-8700K clocked at 3.7GHz (6 physical, 12 logical) with access to 32GB of memory. However memory usage is minimal.

Single thread general performance (8% CPU usage with my spec):

| Words | First Hash | Second Hash | Third Hash | Total Duration | Matched Phrases | Speed |
| ----- | ---------- | ----------- | ---------- | -------------- | --------------- | ----- |
| 1 | - | - | - | 0.03s | 0 | 0 h/s |
| 2 | - | - | - | 0.03s | 0 | 0 h/s |
| 3 | 0.04s | 0.14s | - | 0.15s | 4,543 | 30,287 h/s |
| 4 | 0.26s | 24.57s | 24.88s | 25.20s | 7,175,736 | 284,751 h/s |
| 5 | 1.40s |49.23m |50.38m |51.44m | 1,036,917,884 | 335,923 h/s |


Multi thread duration (Reaches 100% with 12 threads):

| Words | 1 Thread | 4 Threads | 6 Threads | 12 Threads | 24 Threads | 48 Threads | 96 Threads |
| ----- | -------- | --------- | --------- | ---------- | ---------- | ---------- | ---------- |
| 1 | 0.03s | 0.03s | 0.03s | 0.03s | 0.03s | 0.03s | 0.03s |
| 2 | 0.03s | 0.03s | 0.03s | 0.03s | 0.03s | 0.03s | 0.03s |
| 3 | 0.15s | 0.09s | 0.08s | 0.08s | 0.08s | 0.08s | 0.09s |
| 4 | 25.20s | 12.90s | 8.90s | 6.40s | 6.13s | 6.21s | 5.81s |
| 5 | 51.44m | 39.53m | 28.921m | 19.36m | 14.12m | 14.36m | 13.35m |

Optimal configuration for finding all hashes:

| Words | Threads | Duration | Speed |
| ----- | ------- | -------- |------ |
| 3 | 6 | 0.08s | 56,787 h/s |
| 4 | 24 | 6.13s | 1,170,593 h/s |
| 5 | 24 | 14.12m (847s) | 1,223,935 h/s |