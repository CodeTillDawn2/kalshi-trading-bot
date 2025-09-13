@echo off
cd docs
echo Starting local server on http://localhost:8000
echo Opening http://localhost:8000/api/index.html in your default browser
start http://localhost:8000/api/index.html
echo Press Ctrl+C to stop the server and close this window
python -m http.server 8000