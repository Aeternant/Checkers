// Aeternant
// TODO
// create selector
// load menu on victory
// close button (any kind of UI really)
// victory message
// go through commments
// optimized color management

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Board : MonoBehaviour {

    public Camera cam;
    public GameObject gameBoard;
    public GameObject gamePiecePrefab;

    // Board state
    // 2 Dimensional Array X/Y
    // corresponds to unity coordinates
    // 0, 0 is lower left corner
    // contains reference to the relevant piece at that space
    private GameObject[,] state = new GameObject[8, 8];

    // piece currently selected by player
    private GameObject selectedPiece = null;

    // True if it's the player's turn
    private bool playerTurn = true;

    Color playerColor = (Color)new Color32(255, 0, 0, 255);
    Color playerQueenColor = (Color)new Color32(127, 0, 0, 255);
    Color opponentColor = (Color)new Color32(0, 255, 0, 255);
    Color opponentQueenColor = (Color)new Color32(0, 127, 0, 255);

    int takenPlayerPieces = 0;
    int takenOpponentPieces = 0;

    // Awake is called before the first frame update
    void Awake() {
        // populate board
        for (int x = 0; x < 8; x++) {
            // player pieces
            for (int y = 0; y < 3; y++) {
                if ((x + y) % 2 == 0) {
                    this.state[x, y] = Instantiate(gamePiecePrefab, new Vector2(x, y), gameBoard.transform.rotation);
                    this.state[x, y].GetComponent<SpriteRenderer>().color = playerColor;
                }
            }
            // opponent pieces
            for (int y = 5; y < 8; y++) {
                if ((x + y) % 2 == 0) {
                    this.state[x, y] = Instantiate(gamePiecePrefab, new Vector2(x, y), gameBoard.transform.rotation);
                    this.state[x, y].GetComponent<SpriteRenderer>().color = opponentColor;
                }
            }
        }

    }

    // Update is called once per frame
    void Update() {
        if (Input.GetButtonDown("Fire1")) {
            Vector2 mousePosVector = cam.ScreenToWorldPoint(Input.mousePosition);

            (int, int) mousePos = (Mathf.RoundToInt(mousePosVector.x), Mathf.RoundToInt(mousePosVector.y));
            // only do stuff if input conforms to a white square on board
            if (validateInput(ref mousePos)) {
                // player is trying to make a move
                if (selectedPiece) {
                    // attempt to make a move
                    if (attemptMove(mousePos)) {
                        // and change player turn if successful
                        playerTurn = !playerTurn;
                    }
                    // deselect piece
                    selectedPiece = null;
                }
                // player is trying to select a piece
                else {
                    // try to select the piece at the mouse pos
                    selectPiece(mousePos);
                }
            }
        }
        if (takenPlayerPieces == 12 || takenOpponentPieces == 12) {
            SceneManager.LoadScene(1);
        }
    }

    bool validateInput(ref (int x, int y) mousePos) {
        // rasterize input to grid
        mousePos.x = (int)Mathf.Round(mousePos.x);
        mousePos.y = (int)Mathf.Round(mousePos.y);
        // check if in confines of board and a white square
        return -1 < mousePos.x && mousePos.x < 8 && -1 < mousePos.y && mousePos.y < 8 && ((mousePos.x + mousePos.y) % 2 == 0);
    }

    bool attemptMove((int x, int y) to) {
        bool moveSuccess = false;
        // valid move?
        (int x, int y) from;
        from.x = (int)selectedPiece.transform.position.x;
        from.y = (int)selectedPiece.transform.position.y;
        Color32 selectedPieceColor = selectedPiece.GetComponent<SpriteRenderer>().color;
        bool isQueen = playerQueenColor == selectedPieceColor || opponentQueenColor == selectedPieceColor;
        int direction = -1;
        if (playerTurn) {
            direction = 1;
        }
        // 'to' location should be empty
        if (!state[to.x, to.y]) {
            // normal move
            if (Mathf.Abs(to.x - from.x) == 1) {
                if (isQueen) {
                    // direction doesn't matter
                    if (Mathf.Abs(to.y - from.y) == 1) {
                        normalMove(from, to);
                        moveSuccess = true;
                    }
                }
                else if (to.y - from.y == direction) {
                    normalMove(from, to);
                    moveSuccess = true;
                }
            }
            // take move
            else if (Mathf.Abs(to.x - from.x) == 2 && (Mathf.Abs(to.y - from.y) == 2)) {
                // check for takable piece
                (int x, int y) toBeTaken = ((from.x + to.x) / 2, (from.y + to.y) / 2);
                if (state[toBeTaken.x, toBeTaken.y]) {
                    GameObject toBeTakenPiece = state[toBeTaken.x, toBeTaken.y];
                    // check piece color
                    Color32 toBeTakenColor = toBeTakenPiece.GetComponent<SpriteRenderer>().color;
                    if ((playerTurn && (toBeTakenColor == opponentColor || toBeTakenColor == opponentQueenColor)) ||
                        (!playerTurn && (toBeTakenColor == playerColor || toBeTakenColor == playerQueenColor))) {
                        if (isQueen) {
                            moveSuccess = takeMove(from, to, toBeTakenPiece);
                        }
                        else if (to.y - from.y == direction * 2) {
                            moveSuccess = takeMove(from, to, toBeTakenPiece);
                        }

                    }
                }
            }
        }
        // Queen ascension
        if (moveSuccess) {
            if (playerTurn) {
                if (to.y == 7) {
                    selectedPiece.GetComponent<SpriteRenderer>().color = playerQueenColor;
                }
            }
            else {
                if (to.y == 0) {
                    selectedPiece.GetComponent<SpriteRenderer>().color = opponentQueenColor;
                }
            }
        }
        return moveSuccess;
    }

    bool normalMove((int x, int y) from, (int x, int y) to) {
        // remove from board
        state[from.x, from.y] = null;
        // move selected piece
        selectedPiece.transform.position = new Vector3(to.x, to.y);
        // add to board                    
        state[to.x, to.y] = selectedPiece;
        return true;
    }

    bool takeMove((int x, int y) from, (int x, int y) to, GameObject toBeTakenPiece) {
        // move taken piece off board
        if (playerTurn) {
            // blue pieces go left
            toBeTakenPiece.transform.position = new Vector3(-2 - (takenOpponentPieces % 3), 0 + (takenOpponentPieces / 3));
            takenOpponentPieces++;
        }
        else {
            // red piece go right
            toBeTakenPiece.transform.position = new Vector3(9 + (takenPlayerPieces % 3), 0 + (takenPlayerPieces / 3));
            takenPlayerPieces++;
        }
        // remove taken piece from board
        state[(from.x + to.x) / 2, (from.y + to.y) / 2] = null;
        // remove moving piece from board
        state[from.x, from.y] = null;
        // move selected piece
        selectedPiece.transform.position = new Vector3(to.x, to.y);
        // add to board                    
        state[to.x, to.y] = selectedPiece;
        return true;
    }

    void selectPiece((int x, int y) mousePos) {
        // check if there is a piece there
        if (state[mousePos.x, mousePos.y]) {
            // make sure it's the turn player's color
            Color32 selectedPieceColor = state[mousePos.x, mousePos.y].GetComponent<SpriteRenderer>().color;
            if (playerTurn) {
                if (selectedPieceColor == playerColor || selectedPieceColor == playerQueenColor) {
                    // set piece as selected
                    selectedPiece = state[mousePos.x, mousePos.y];
                }
            }
            else {
                if (selectedPieceColor == opponentColor || selectedPieceColor == opponentQueenColor) {
                    selectedPiece = state[mousePos.x, mousePos.y];
                }
            }
        }
    }
}