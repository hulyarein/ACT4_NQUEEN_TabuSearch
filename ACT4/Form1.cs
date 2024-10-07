using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ACT4
{
    public partial class Form1 : Form
    {
        int side;
        int n = 6;
        SixState startState;
        SixState currentState;
        int moveCounter;

        int[,] hTable;
        ArrayList bMoves;
        Object chosenMove;

        List<SixState> tabuList;
        const int tabuTenure = 5;
        const int maxIterations = 1000;

        SixState bestState;
        int bestHeuristic;

        public Form1()
        {
            InitializeComponent();

            side = pictureBox1.Width / n;

            startState = randomSixState();
            currentState = new SixState(startState);
            bestState = new SixState(currentState);
            bestHeuristic = getAttackingPairs(startState);

            tabuList = new List<SixState>();

            updateUI();
            label1.Text = "Attacking pairs: " + getAttackingPairs(startState);
        }

        private void updateUI()
        {
            pictureBox2.Refresh();

            label3.Text = "Attacking pairs: " + getAttackingPairs(currentState);
            label4.Text = "Moves: " + moveCounter;
            hTable = getHeuristicTableForPossibleMoves(currentState);
            bMoves = getBestMoves(hTable);

            listBox1.Items.Clear();
            foreach (Point move in bMoves)
            {
                listBox1.Items.Add(move);
            }

            if (bMoves.Count > 0)
                chosenMove = chooseMove(bMoves);
            label2.Text = "Chosen move: " + chosenMove;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        e.Graphics.FillRectangle(Brushes.Blue, i * side, j * side, side, side);
                    }
                    if (j == startState.Y[i])
                        e.Graphics.FillEllipse(Brushes.Fuchsia, i * side, j * side, side, side);
                }
            }
        }

        private void pictureBox2_Paint(object sender, PaintEventArgs e)
        {
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if ((i + j) % 2 == 0)
                    {
                        e.Graphics.FillRectangle(Brushes.Black, i * side, j * side, side, side);
                    }
                    if (j == currentState.Y[i])
                        e.Graphics.FillEllipse(Brushes.Fuchsia, i * side, j * side, side, side);
                }
            }
        }

        private SixState randomSixState()
        {
            Random r = new Random();
            SixState random = new SixState(r.Next(n),
                                             r.Next(n),
                                             r.Next(n),
                                             r.Next(n),
                                             r.Next(n),
                                             r.Next(n));

            return random;
        }

        private int getAttackingPairs(SixState f)
        {
            int attackers = 0;

            for (int rf = 0; rf < n; rf++)
            {
                for (int tar = rf + 1; tar < n; tar++)
                {
                    if (f.Y[rf] == f.Y[tar])
                        attackers++;
                }
                for (int tar = rf + 1; tar < n; tar++)
                {
                    if (f.Y[tar] == f.Y[rf] + tar - rf)
                        attackers++;
                }
                for (int tar = rf + 1; tar < n; tar++)
                {
                    if (f.Y[rf] == f.Y[tar] + tar - rf)
                        attackers++;
                }
            }

            return attackers;
        }

        private int[,] getHeuristicTableForPossibleMoves(SixState thisState)
        {
            int[,] hStates = new int[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    SixState possible = new SixState(thisState);
                    possible.Y[i] = j;
                    hStates[i, j] = getAttackingPairs(possible);
                }
            }

            return hStates;
        }

        private ArrayList getBestMoves(int[,] heuristicTable)
        {
            ArrayList bestMoves = new ArrayList();
            int bestHeuristicValue = heuristicTable[0, 0];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    if (bestHeuristicValue > heuristicTable[i, j])
                    {
                        bestHeuristicValue = heuristicTable[i, j];
                        bestMoves.Clear();
                        if (currentState.Y[i] != j)
                            bestMoves.Add(new Point(i, j));
                    }
                    else if (bestHeuristicValue == heuristicTable[i, j])
                    {
                        if (currentState.Y[i] != j)
                            bestMoves.Add(new Point(i, j));
                    }
                }
            }
            label5.Text = "Possible Moves (H=" + bestHeuristicValue + ")";
            return bestMoves;
        }

        private Object chooseMove(ArrayList possibleMoves)
        {
            possibleMoves.Sort(new MoveComparer(currentState, this));

            SixState bestMoveState = null;
            Point bestMove = new Point();
            int bestHeuristic = int.MaxValue;

            foreach (Point move in possibleMoves)
            {
                SixState newState = new SixState(currentState);
                newState.Y[move.X] = move.Y;
                int newHeuristic = getAttackingPairs(newState);

                if (newHeuristic < bestHeuristic)
                {
                    bestHeuristic = newHeuristic;
                    bestMoveState = newState;
                    bestMove = move;
                }

                if (!IsTabu(newState) || newHeuristic < getAttackingPairs(currentState))
                {
                    tabuList.Add(new SixState(currentState));
                    if (tabuList.Count > tabuTenure)
                    {
                        tabuList.RemoveAt(0);
                    }
                    return move;
                }
            }

            if (bestMoveState != null)
            {
                tabuList.Add(new SixState(currentState));
                if (tabuList.Count > tabuTenure)
                {
                    tabuList.RemoveAt(0);
                }
                return bestMove;
            }

            return possibleMoves[0]; 
        }


        private class MoveComparer : IComparer
        {
            private SixState currentState;
            private Form1 form;

            public MoveComparer(SixState currentState, Form1 form)
            {
                this.currentState = currentState;
                this.form = form;
            }

            public int Compare(object x, object y)
            {
                Point moveA = (Point)x;
                Point moveB = (Point)y;

                SixState stateA = new SixState(currentState);
                stateA.Y[moveA.X] = moveA.Y;
                int heuristicA = form.getAttackingPairs(stateA);

                SixState stateB = new SixState(currentState);
                stateB.Y[moveB.X] = moveB.Y;
                int heuristicB = form.getAttackingPairs(stateB);

                return heuristicA.CompareTo(heuristicB);
            }
        }

        private bool IsTabu(SixState state)
        {
            return tabuList.Any(tabuState => tabuState.Y.SequenceEqual(state.Y));
        }

        private void executeMove(Point move)
        {
            for (int i = 0; i < n; i++)
            {
                startState.Y[i] = currentState.Y[i];
            }
            currentState.Y[move.X] = move.Y;
            moveCounter++;

            int currentHeuristic = getAttackingPairs(currentState);
            if (currentHeuristic < bestHeuristic)
            {
                bestHeuristic = currentHeuristic;
                bestState = new SixState(currentState);
            }

            chosenMove = null;
            updateUI();
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (getAttackingPairs(currentState) > 0)
                executeMove((Point)chosenMove);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            startState = randomSixState();
            currentState = new SixState(startState);

            moveCounter = 0;

            updateUI();
            pictureBox1.Refresh();
            label1.Text = "Attacking pairs: " + getAttackingPairs(startState);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            int iterations = 0;
            int noImprovementCounter = 0;
            int bestKnownAttackingPairs = getAttackingPairs(currentState);

            while (getAttackingPairs(currentState) > 0 && iterations < maxIterations)
            {
                if (chosenMove == null || getAttackingPairs(currentState) <= 0)
                {
                    break;
                }

                SixState newState = new SixState(currentState);
                newState.Y[((Point)chosenMove).X] = ((Point)chosenMove).Y;

                if (!newState.Y.SequenceEqual(currentState.Y))
                {
                    executeMove((Point)chosenMove);

                    int currentAttackingPairs = getAttackingPairs(currentState);
                    if (currentAttackingPairs < bestKnownAttackingPairs)
                    {
                        bestKnownAttackingPairs = currentAttackingPairs;
                        noImprovementCounter = 0;
                    }
                    else
                    {
                        noImprovementCounter++;
                    }
                }
                else
                {
                    MessageBox.Show("No progress made. The algorithm may be stuck in a loop.");
                    break;
                }

                iterations++;


                if (noImprovementCounter >= 10) 
                {
                    MessageBox.Show("No improvement for 10 moves. Random restart triggered.");
                    currentState = randomSixState();
                    noImprovementCounter = 0;
                }
            }

            if (iterations >= maxIterations)
            {
                MessageBox.Show("Max iterations reached. The algorithm may be stuck in a loop.");
            }
        }


        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
