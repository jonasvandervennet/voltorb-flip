using System.Collections.Generic;
using System.Drawing;

namespace voltorbflip
{
    class NumberDetection
    {
        public static int ExtractNumberFromBitmap(Bitmap bitmap, int x_topleft, int x_botright, int y_topleft, int y_botright)
        {
            List<List<Color>> area = new List<List<Color>>();
            for (int y = y_topleft; y < y_botright; y++)
            {
                var row = new List<Color>();
                for (int x = x_topleft; x < x_botright; x++)
                {
                    Color filteredColor = bitmap.GetPixel(x, y) == VoltorbFlipColors._BLACK ? VoltorbFlipColors._BLACK : VoltorbFlipColors._WILDCARD;
                    row.Add(filteredColor);
                }
                area.Add(row);
            }

            if (Detect0(area)) { return 0; }
            if (Detect1(area)) { return 1; }
            if (Detect2(area)) { return 2; }
            if (Detect3(area)) { return 3; }
            if (Detect4(area)) { return 4; }
            if (Detect5(area)) { return 5; }
            if (Detect6(area)) { return 6; }
            if (Detect7(area)) { return 7; }
            if (Detect9(area)) { return 9; }
            return 8;
        }

        private static bool Detect0(List<List<Color>> area)
        {
            // DETECT 0
            // first and last column are same, (or first two columns equal the last two)
            // AND (to distinguish from 8), there is NO "BLACK - BACKGROUND - BLACK - BACKGROUND - END BLACK" column
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            for (int i = 0; i < num_rows; i++)
            {
                if (area[i][0] != area[i][num_cols - 1]) { return false; }
            }
            for (int i = 0; i < num_cols; i++)
            {
                Color prev = area[0][i];
                if (prev != VoltorbFlipColors._BLACK) { continue; }
                int n_transitions = 0;
                for (int j = 1; j < num_rows; j++)
                {
                    if (area[j][i] != prev) { prev = area[j][i]; n_transitions++; }
                }
                if (n_transitions == 4) { return false; }
            }
            return true;
        }
        private static bool Detect1(List<List<Color>> area)
        {
            // DETECT 1
            // columns are either "BACKGROUND - BLACK - BACKGROUND - END BLACK", full BLACK or "BACKGROUND - END BLACK"
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            for (int i = 0; i < num_cols; i++)
            {
                Color prev = area[0][i];
                int n_transitions = 0;
                for (int j = 1; j < num_rows; j++)
                {
                    if (area[j][i] != prev) { prev = area[j][i]; n_transitions++; }
                }
                if (area[0][i] == VoltorbFlipColors._WILDCARD && !(n_transitions == 1 || n_transitions == 3)) { return false; }
                if (area[0][i] == VoltorbFlipColors._BLACK && n_transitions > 0) { return false; }
            }
            return true;
        }
        private static bool Detect2(List<List<Color>> area)
        {
            // DETECT 2
            // 1 has this property too, but is already eliminated as a possibility
            // at least one column will be "BACKGROUND - BLACK - BACKGROUND - END IN BLACK"
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            for (int i = 0; i < num_cols; i++)
            {
                Color prev = area[0][i];
                if (prev != VoltorbFlipColors._WILDCARD) { continue; }
                int n_transitions = 0;
                for (int j = 1; j < num_rows; j++)
                {
                    if (area[j][i] != prev) { prev = area[j][i]; n_transitions++; }
                }
                if (n_transitions == 3) { return true; }
            }
            return false;
        }

        private static bool Detect3(List<List<Color>> area)
        {
            // DETECT 3
            //// TODO: watch out for potential mismatch with 7!
            //// any middle row (even/odd rounding included) will be "BACKGROUND - END IN BLACK" with black longer than background
            // BETTER: symmetrical in rows,but not in columns (up to 1 mistake with odd number of cols/rows, resulting in 2 errors counted)
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            int[] symmetry_errors_row_col = new int[2] { 0, 0 };
            for (int i = 0; i < num_rows / 2; i++)
            {
                for (int j = 0; j < num_cols; j++)
                {
                    if (area[i][j] != area[num_rows - 1 - i][j])
                    {
                        symmetry_errors_row_col[0]++; break;
                    }
                }
            }
            for (int i = 0; i < num_rows; i++)
            {
                for (int j = 0; j < num_cols / 2; j++)
                {
                    if (area[i][j] != area[i][num_cols - 1 - j]) { symmetry_errors_row_col[1]++; break; }
                }
            }
            return symmetry_errors_row_col[0] <= 2 && symmetry_errors_row_col[1] > 2;

        }

        private static bool Detect4(List<List<Color>> area)
        {
            // DETECT 4
            // contains both column for "BLACK - END IN BACKGROUND" and "BACKGROUND - END IN BLACK"
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            int checks = 0;
            foreach (Color init in new Color[2] { VoltorbFlipColors._WILDCARD, VoltorbFlipColors._BLACK })
            {
                for (int i = 0; i < num_cols; i++)
                {
                    int n_transitions = 0;
                    Color prev = area[0][i];
                    if (prev != init) { continue; }  // look at each starting color in order
                    for (int j = 1; j < num_rows; j++)
                    {
                        if (prev != area[j][i]) { n_transitions++; prev = area[j][i]; }
                    }
                    if (n_transitions == 1) { checks++; break; }
                }
            }
            return checks == 2;
        }

        private static bool Detect5(List<List<Color>> area)
        {
            // DETECT 5
            // TODO: unsure of accuracy
            // 1st and last column "BLACK - BACKGROUND - BLACK - END IN BACKGROUND"
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            foreach (int i in new int[] { 0, num_cols - 1 })
            {
                int n_transitions = 0;
                Color prev = area[0][i];
                if (prev != VoltorbFlipColors._BLACK) { return false; }
                for (int j = 1; j < num_rows; j++)
                {
                    if (prev != area[j][i]) { n_transitions++; prev = area[j][i]; }
                }
                if (n_transitions != 3) { return false; }
            }
            return true;
        }

        private static bool Detect6(List<List<Color>> area)
        {
            // DETECT 6
            // first column with BLACK in it is "BACKGROUND - BLACK - END IN BACHGROUND"
            // last column with BLACK in it is "BACKGROUND - BLACK - BACKGROUND - BLACK - END IN BACHGROUND"
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            foreach ((int, int) to_match in new (int, int)[] { (0, 2), (num_cols - 1, 4) })
            {
                int i = to_match.Item1;
                int goal_transitions = to_match.Item2;
                int n_transitions = 0;
                Color prev = area[0][i];
                if (prev != VoltorbFlipColors._WILDCARD) { return false; }
                for (int j = 1; j < num_rows; j++)
                {
                    if (prev != area[j][i]) { n_transitions++; prev = area[j][i]; }
                }
                if (n_transitions != goal_transitions) { return false; }
            }
            return true;
        }

        private static bool Detect7(List<List<Color>> area)
        {
            // DETECT 7
            // first row that is not fully BLACK is "BLACK - BACKGROUND - END IN BLACK"
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            for (int i = 0; i < num_rows; i++)
            {
                int n_transitions = 0;
                Color prev = area[i][0];
                if (prev != VoltorbFlipColors._BLACK) { return false; }
                for (int j = 1; j < num_cols; j++)
                {
                    if (prev != area[i][j]) { n_transitions++; prev = area[i][j]; }
                }
                if (n_transitions == 0) { continue; }
                return n_transitions == 2;
            }
            return false;
        }

        private static bool Detect9(List<List<Color>> area)
        {
            // DETECT 9
            // reverse of 6
            // last column with BLACK in it is "BACKGROUND - BLACK - END IN BACHGROUND"
            // first column with BLACK in it is "BACKGROUND - BLACK - BACKGROUND - BLACK - END IN BACHGROUND"
            int num_rows = area.Count;
            int num_cols = area[0].Count;
            foreach ((int, int) to_match in new (int, int)[] { (num_cols - 1, 2), (0, 4) })
            {
                int i = to_match.Item1;
                int goal_transitions = to_match.Item2;
                int n_transitions = 0;
                Color prev = area[0][i];
                if (prev != VoltorbFlipColors._WILDCARD) { return false; }
                for (int j = 1; j < num_rows; j++)
                {
                    if (prev != area[j][i]) { n_transitions++; prev = area[j][i]; }
                }
                if (n_transitions != goal_transitions) { return false; }
            }
            return true;
        }
    }
}
