from flask import Flask, request
from flask_cors import CORS, cross_origin

from main import hello_world

if __name__ == "__main__":
    app = Flask(__name__)

    CORS(app)

    @app.route('/', methods=['GET', 'POST'])
    def index():
        return hello_world(request)

    app.run('127.0.0.1', 5000, debug=True)
