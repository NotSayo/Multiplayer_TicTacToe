import numpy as np
import tensorflow as tf
import random
from flask import Flask, request, jsonify
from tensorflow.keras.models import Sequential
from tensorflow.keras.layers import Dense
from tensorflow.keras.optimizers import Adam
from tensorflow.keras.utils import to_categorical
import pickle
import os

FILENAME = "model.pkl"

# Tic-Tac-Toe board representation
def empty_board():
    return np.zeros(9, dtype=int)

# Check for a winner
def check_winner(board):
    win_patterns = [(0,1,2), (3,4,5), (6,7,8),
                    (0,3,6), (1,4,7), (2,5,8),
                    (0,4,8), (2,4,6)]
    for pattern in win_patterns:
        if board[pattern[0]] == board[pattern[1]] == board[pattern[2]] != 0:
            return board[pattern[0]]  # Return winner (1 or -1)
    return 0  # No winner

# Get available moves
def available_moves(board):
    return [i for i in range(9) if board[i] == 0]

# Minimax Algorithm to find best move
def minimax(board, is_maximizing):
    winner = check_winner(board)
    if winner != 0:
        return winner * 10  # +10 for AI win, -10 for opponent win
    if 0 not in board:
        return 0  # Draw

    scores = []
    for move in available_moves(board):
        board[move] = 1 if is_maximizing else -1
        score = minimax(board, not is_maximizing)
        board[move] = 0  # Undo move
        scores.append(score)

    return max(scores) if is_maximizing else min(scores)
model = None
# Train the model
if os.path.exists(FILENAME):
    with open(FILENAME, "rb") as f:
        model = pickle.load(f)
    print("Loaded data from pickle:", model)
else:
    # Generate training data
    X_train = []
    y_train = []

    for _ in range(5000):  # Generate 5000 game states
        board = empty_board()
        moves = random.sample(range(9), random.randint(3, 9))  # Random game state
        for i, move in enumerate(moves):
            board[move] = 1 if i % 2 == 0 else -1
            if check_winner(board) != 0:
                break  # Stop if game is over

        # AI's turn: Find best move
        best_move = None
        best_score = -float('inf')

        for move in available_moves(board):
            board[move] = 1  # AI plays
            score = minimax(board, False)
            board[move] = 0  # Undo move
            if score > best_score:
                best_score = score
                best_move = move

        if best_move is not None:
            X_train.append(board.copy())
            y_train.append(best_move)

    X_train = np.array(X_train)
    y_train = np.array(y_train)

    print(f"Generated {len(X_train)} training samples")

    # Convert move labels to categorical (one-hot encoding)
    y_train_categorical = to_categorical(y_train, num_classes=9)

    # Define the neural network model
    model = Sequential([
        Dense(64, activation='relu', input_shape=(9,)),  # Input: 9 board positions
        Dense(64, activation='relu'),
        Dense(9, activation='softmax')  # Output: probabilities for 9 moves
    ])

    # Compile the model
    model.compile(optimizer=Adam(learning_rate=0.001),
                  loss='categorical_crossentropy',
                  metrics=['accuracy'])

    # Train the model
    model.fit(X_train, y_train_categorical, epochs=50, batch_size=32)

    pickle.dump(model, open('model.pkl', 'wb'))


def ai_move(board):
    board_input = np.array(board).reshape(1, 9)
    move_probs = model.predict(board_input)[0]
    best_move = np.argmax(move_probs)  # Choose the best move
    if board[best_move] == 0:
        board[best_move] = 1  # AI moves
    else:
        print("Invalid move predicted!")  # This should rarely happen
    return board



app = Flask(__name__)

@app.route('/move', methods=['POST'])
def recieve_move():
    board = request.json['board']
    board = np.array(board)
    move = ai_move(board)
    return move.tolist()




app.run(port=5000)