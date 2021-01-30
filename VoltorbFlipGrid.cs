using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace voltorbflip
{
    enum CardType
    {
        Voltorb = 0, One = 1, Two = 2, Three = 3, Unknown = 4, Safe, Useless
    };

    [Serializable]
    class VoltorbFlipGrid
    {
        CardType[,] grid;
        (int, int)[] col_constraints;
        (int, int)[] row_constraints;

        static CardType[] FILLED_CARDTYPES = new CardType[] { CardType.Voltorb, CardType.One, CardType.Two, CardType.Three };

        public VoltorbFlipGrid(List<(int, int)> constraints)
        {
            col_constraints = new (int, int)[5] { constraints[0], constraints[1], constraints[2], constraints[3], constraints[4] };
            row_constraints = new (int, int)[5] { constraints[5], constraints[6], constraints[7], constraints[8], constraints[9] };
            grid = new CardType[5, 5] {
                { CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, },
                { CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, },
                { CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, },
                { CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, },
                { CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, CardType.Unknown, },
            };
        }

        CardType[] Get_col(int i)
        {
            return new CardType[5] { grid[0, i], grid[1, i], grid[2, i], grid[3, i], grid[4, i], };
        }

        CardType[] Get_row(int i)
        {
            return new CardType[5] { grid[i, 0], grid[i, 1], grid[i, 2], grid[i, 3], grid[i, 4], };
        }

        public CardType Get(int i, int j)
        {
            return grid[i, j];
        }

        public void Set(int i, int j, CardType content)
        {
            grid[i, j] = content;
        }

        public void Set(int i, int j, int content)
        {
            CardType type;
            switch (content)
            {
                case 1:
                    type = CardType.One;
                    break;
                case 2:
                    type = CardType.Two;
                    break;
                case 3:
                    type = CardType.Three;
                    break;
                default:
                    return;
            }
            grid[i, j] = type;
        }

        bool IsPossibleArray(CardType[] arr, int goal_points, int goal_voltorbs)
        {
            int num_voltorbs = 0;
            int num_unknowns = 0;
            int current_points = 0;
            for (int i = 0; i < 5; i++)
            {
                switch (arr[i])
                {
                    case CardType.Unknown:
                        num_unknowns++;
                        break;
                    case CardType.Voltorb:
                        num_voltorbs++;
                        break;
                    default:
                        current_points += (int)arr[i];
                        break;
                }
            }
            if (num_voltorbs > goal_voltorbs || num_voltorbs + num_unknowns < goal_voltorbs) { return false; }
            else if (current_points > goal_points || current_points + 3 * num_unknowns < goal_points) { return false; }
            else { return true; }
        }

        bool Possible()
        {
            for (int i = 0; i < 5; i++)
            {
                if (!IsPossibleArray(Get_row(i), row_constraints[i].Item1, row_constraints[i].Item2)) { return false; }
                if (!IsPossibleArray(Get_col(i), col_constraints[i].Item1, col_constraints[i].Item2)) { return false; }
            }
            return true;
        }

        bool Valid()
        {
            if (!Possible()) { return false; }
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (grid[i, j] == CardType.Unknown) { return false; }
                }
            }
            return true;
        }

        List<VoltorbFlipGrid> GetAllSolutions()
        {
            List<VoltorbFlipGrid> solutions = new List<VoltorbFlipGrid>();
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    CardType current = this.grid[i, j];
                    if (current != CardType.Unknown) { continue; }
                    foreach (CardType option in FILLED_CARDTYPES)
                    {
                        this.grid[i, j] = option;
                        if (!Possible()) { continue; }
                        if (Valid()) { solutions.Add(this.Clone());break; }
                        else { solutions.AddRange(GetAllSolutions()); }
                    }
                    // reset
                    this.grid[i, j] = CardType.Unknown;
                    return solutions;
                }
            }
            return solutions;
        }

        public (CardType[,],float[,]) GetFixPoints()
        {
            List<VoltorbFlipGrid> solutions = this.Clone().GetAllSolutions();
            HashSet<CardType>[,] total = new HashSet<CardType>[5, 5];
            CardType[,] aggregate = new CardType[5, 5];
            (int,int)[,] option_occurences = new (int, int)[5, 5];
            float[,] probability_useful = new float[5, 5]; // probability a card is either two or three, used for highlighting the most useful unknown card in the grid
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    total[i, j] = new HashSet<CardType>();
                    option_occurences[i, j] = (0,0);
                    probability_useful[i, j] = 0;
                }
            }
            foreach (var solution in solutions)
            {
                for (int i = 0; i < 5; i++)
                {
                    for (int j = 0; j < 5; j++)
                    {
                        var card = solution.Get(i, j);
                        total[i, j].Add(card);
                        option_occurences[i, j].Item1++;
                        if (card==CardType.Two || card==CardType.Three) { option_occurences[i,j].Item2++; }
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    probability_useful[i, j] = (float)option_occurences[i, j].Item2 / (float)option_occurences[i, j].Item1;
                }
            }

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var options = total[i, j];
                    switch (options.Count)
                    {
                        case 1:
                            foreach (var option in FILLED_CARDTYPES)
                            {
                                if (options.Contains(option)) { aggregate[i, j] = option; break; }
                            }
                            break;
                        case 2:
                            aggregate[i, j] = options.Contains(CardType.Voltorb) ? (options.Contains(CardType.One)) ? CardType.Useless : CardType.Unknown : CardType.Safe;
                            break;
                        default:
                            aggregate[i, j] = options.Contains(CardType.Voltorb) ? CardType.Unknown : CardType.Safe;
                            break;
                    }
                    // only 'unknown' cards are have to be guessed
                    if (aggregate[i, j] != CardType.Unknown) { probability_useful[i, j] = 0; }
                }
            }
            return (aggregate, probability_useful);
        }

        // https://dotnetcoretutorials.com/2020/09/09/cloning-objects-in-c-and-net-core/
        VoltorbFlipGrid Clone()
        {
            IFormatter formatter = new BinaryFormatter();
            using (MemoryStream stream = new MemoryStream())
            {
                formatter.Serialize(stream, this);
                stream.Seek(0, SeekOrigin.Begin);
                return (VoltorbFlipGrid)formatter.Deserialize(stream);
            }
        }
    }
}
