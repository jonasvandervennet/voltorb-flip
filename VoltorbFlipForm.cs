using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace voltorbflip
{
    public partial class VoltorbFlipForm : Form
    {
        List<(int, int)> constraints;
        PictureBox[,] gridDisplay;
        float[,] usefulness_prob;
        Bitmap content;
        VoltorbFlipGrid grid;
        int screenIndex = -1;
        ((int, int), (int, int)) region_markers = ((-1, -1), (-1, -1));

        public VoltorbFlipForm()
        {
            InitializeComponent();
            gridDisplay = new PictureBox[5, 5]
            {
                {pictureBox1,pictureBox2,pictureBox3,pictureBox4,pictureBox5 },
                {pictureBox6,pictureBox7,pictureBox8,pictureBox9,pictureBox10 },
                {pictureBox11,pictureBox12,pictureBox13,pictureBox14,pictureBox15 },
                {pictureBox16,pictureBox17,pictureBox18,pictureBox19,pictureBox20 },
                {pictureBox21,pictureBox22,pictureBox23,pictureBox24,pictureBox25 },
            };
            usefulness_prob = new float[5, 5];
        }

        private void VoltorbFlipForm_Load(object sender, EventArgs e)
        {
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    gridDisplay[i, j].Image = Properties.Resources.questionmark;
                    usefulness_prob[i, j] = 0;
                }
            }
        }

        private void UpdateGridDisplay()
        {
            var (safe, prob) = this.grid.GetFixPoints();
            this.usefulness_prob = prob;
            UpdateGridDisplay(safe);
        }

        private void UpdateGridDisplay(CardType[,] good_spots)
        {
            (int, int) best_coords = (0, 0);
            float max_prob = 0; ;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (usefulness_prob[i, j] > max_prob)
                    {
                        max_prob = usefulness_prob[i, j];
                        best_coords = (i, j);
                    }
                }
            }

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    // https://docs.microsoft.com/en-us/dotnet/desktop/winforms/controls/how-to-set-pictures-at-run-time-windows-forms?view=netframeworkdesktop-4.8
                    gridDisplay[i, j].Image.Dispose();
                    if ((i, j) == best_coords && max_prob > 0) { gridDisplay[i, j].Image = Properties.Resources.gold_questionmark; }
                    else
                    {
                        switch (good_spots[i, j])
                        {
                            case CardType.One:
                                gridDisplay[i, j].Image = Properties.Resources.one;
                                break;
                            case CardType.Two:
                                gridDisplay[i, j].Image = Properties.Resources.two;
                                break;
                            case CardType.Three:
                                gridDisplay[i, j].Image = Properties.Resources.three;
                                break;
                            case CardType.Voltorb:
                                gridDisplay[i, j].Image = Properties.Resources.voltorb;
                                break;
                            case CardType.Unknown:
                                gridDisplay[i, j].Image = Properties.Resources.questionmark;
                                break;
                            case CardType.Safe:
                                gridDisplay[i, j].Image = Properties.Resources.positive;
                                break;
                            case CardType.Useless:
                                gridDisplay[i, j].Image = Properties.Resources.useless;
                                break;
                        }
                    }
                }
            }
        }

        private (bool, int, int) ContainsMarker(Bitmap content, Color[,] marker, bool usePreviousLocationData)
        {
            int i_start, j_start, i_end, j_end;
            if (usePreviousLocationData && this.region_markers != ((-1, -1), (-1, -1)))
            {
                ((i_start, j_start), (i_end, j_end)) = this.region_markers;
                i_end+=2; // add two dimensions to include bottom right corner size
                j_end+=2;
            } else
            {
                ((i_start, j_start), (i_end, j_end)) = ((0,0),(content.Width, content.Height));
            }

            var size = (int)Math.Sqrt(marker.Length);  // markers are square matrices of colors
            for (int i = i_start; i < i_end-size; i++)
            {
                for (int j = j_start; j < j_end-size; j++)
                {
                    bool valid = true;
                    for (int ii = 0; ii < size; ii++)
                    {
                        for (int jj = 0; jj < size; jj++)
                        {
                            if (content.GetPixel(i + ii, j + jj) != marker[jj, ii])
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) { break; }
                    }
                    if (valid) { return (true, i, j); }
                }

            }
            return (false, 0, 0);
        }

        private List<(int, int)> AllMarkerPositionsBetween(Bitmap content, Color[,] marker, int x_topleft, int x_botright, int y_topleft, int y_botright)
        {
            List<(int, int)> positions = new List<(int, int)>();
            var size = Math.Sqrt(marker.Length);  // markers are square matrices of colors
            for (int i = x_topleft; i < x_botright - size; i++)
            {
                for (int j = y_topleft; j < y_botright - size; j++)
                {
                    bool valid = true;
                    for (int ii = 0; ii < size; ii++)
                    {
                        for (int jj = 0; jj < size; jj++)
                        {
                            if (content.GetPixel(i + ii, j + jj) != marker[jj, ii])
                            {
                                valid = false;
                                break;
                            }
                        }
                        if (!valid) { break; }
                    }
                    if (valid) { positions.Add((i, j)); }
                }

            }
            return positions;
        }

        private void CellEnterEvent(object sender, EventArgs e)
        {
            PictureBox box = (PictureBox)sender;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (this.gridDisplay[i, j] == box) 
                    { this.textBox1.Text = String.Format("{0:n2}%", this.usefulness_prob[i, j]*100); }
                }
            }
        }

        private void CellLeaveEvent(object sender, EventArgs e)
        {
            PictureBox box = (PictureBox)sender;
            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    if (this.gridDisplay[i, j] == box) 
                    { this.textBox1.Text = ""; }
                }
            }
        }

        private void GrabScreenForMarkers(int i, bool usePreviousLocationData)
        {
            Screen screen = Screen.AllScreens[i];
            Rectangle captureRectangle = screen.Bounds;
            Bitmap captureBitmap = new Bitmap(captureRectangle.Width, captureRectangle.Height, PixelFormat.Format32bppArgb);
            Graphics captureGraphics = Graphics.FromImage(captureBitmap);
            captureGraphics.CopyFromScreen(captureRectangle.Left, captureRectangle.Top, 0, 0, captureRectangle.Size);

            var (found_topleft, x_tl, y_tl) = ContainsMarker(captureBitmap, VoltorbFlipColors.TOPLEFT, usePreviousLocationData);
            var (found_botright, x_br, y_br) = ContainsMarker(captureBitmap, VoltorbFlipColors.BOTTOMRIGHT, usePreviousLocationData);
            if (found_topleft && found_botright)
            {

                int x_topleft = x_tl;
                int y_topleft = y_tl;

                int br_size = (int)Math.Sqrt(VoltorbFlipColors.BOTTOMRIGHT.Length);
                int x_botright = x_br + br_size - 1;
                int y_botright = y_br + br_size - 1;

                this.region_markers = ((x_topleft, y_topleft), (x_botright, y_botright));
                this.screenIndex = i;
                this.content = captureBitmap;
            }
            else
            {
                captureGraphics.Dispose();
                captureBitmap.Dispose();
                this.screenIndex = -1;
            }
        }

        private void StartDetectionButton_Click(object sender, EventArgs e)
        {
            // TODO: once the correct screen was found, don't look at others
            if (this.screenIndex != -1)
            {
                // have some record of a previous hit
                GrabScreenForMarkers(this.screenIndex, true);
            }
            // if previous hit was not valid, try all screens
            if (this.screenIndex == -1)
            {
                for (int i = 0; i < Screen.AllScreens.Length; i++)
                {
                    // adapted from: https://www.c-sharpcorner.com/UploadFile/2d2d83/how-to-capture-a-screen-using-C-Sharp/
                    GrabScreenForMarkers(i, false);
                    if (this.screenIndex != -1) { break; }
                }
            }

            if (this.screenIndex == -1) { return; } // no valid screen found

            // Found a screen containing the markers
            var ((x_topleft, y_topleft), (x_botright, y_botright)) = this.region_markers;

            /* IDEA: 
             * - go through section between markers
             * - look at each row, get coordinate of the topleft of the lowest row of white outlines (row of clues)
             * - look at each column, get coordinate of the topleft of the rightmost column of white outlines (column of clues)
             * - parse sideways (left to right) to extract (between white lines) continuous sections with any black pixels
             * - infer number in extracted pixels using number detection rules outlined in this project
             * - go back (right to left), but one white section lower (in same box) to extract last number from box in the same way
             * - move to next box (to left or below)
            */
            var (row_x, row_y) = GetLastBoxesRow(this.content, x_topleft, x_botright, y_topleft, y_botright);
            List<int> bottom_row_tips = ReadBox(this.content, row_x, x_botright, row_y, y_botright);

            var (col_x, col_y, y_increments) = GetLastBoxesCol(this.content, x_topleft, x_botright, y_topleft, y_botright);
            List<int> right_col_tips = new List<int>();
            for (int i = 0; i < 5; i++)
            {
                int y_start = i == 0 ? col_y : col_y + y_increments[i - 1];
                List<int> nums = ReadBox(this.content, col_x, x_botright, y_start, y_botright);
                right_col_tips.AddRange(nums);
            }
            this.constraints = GatherConstraints(bottom_row_tips, right_col_tips);
            this.grid = new VoltorbFlipGrid(constraints);
            ReadGridContent(false);
            UpdateGridDisplay();
        }

        private List<(int, int)> GatherConstraints(List<int> bottom_row_tips, List<int> right_col_tips)
        {
            List<(int, int)> constraints = new List<(int, int)>();

            for (int i = 0; i < 5; i++)
            {
                constraints.Add((bottom_row_tips[2*i] * 10 + bottom_row_tips[2*i + 1], bottom_row_tips[i + 10]));
            }

            for (int i = 0; i < 15; i+=3)
            {
                constraints.Add((right_col_tips[i] * 10 + right_col_tips[i + 1], right_col_tips[i + 2]));
            }
            return constraints;
        }

        // Reads a box or horizontal arrangement of boxes
        private List<int> ReadBox(Bitmap bitmap, int x_topleft, int x_botright, int y_topleft, int y_botright)
        {
            // decide height of number region
            int number_height = 0;
            for (int y = y_topleft; y < y_botright; y++)
            {
                Color current = bitmap.GetPixel(x_topleft, y);
                if (current == VoltorbFlipColors._WHITE) { break; }
                number_height++;
            }

            bool stop = false;
            bool black_started = false;
            int black_start_x = -1;
            List<int> numbers_extracted = new List<int>();
            for (int x = x_topleft; x < x_botright; x++)
            {
                bool contains_black = false;
                for (int y = y_topleft; y < y_topleft + number_height; y++)
                {
                    Color current = bitmap.GetPixel(x, y);
                    if (current == VoltorbFlipColors._BLUE || current == VoltorbFlipColors._DARKGREEN) { stop = true; break; }
                    if (current == VoltorbFlipColors._BLACK) { contains_black = true; }

                }
                if (stop) { break; }
                if (contains_black && !black_started) { black_started = true; black_start_x = x; }
                else if (!contains_black && black_started)
                {
                    black_started = false;
                    int num = NumberDetection.ExtractNumberFromBitmap(bitmap, black_start_x, x, y_topleft, y_topleft + number_height);
                    numbers_extracted.Add(num);
                }
            }


            // determine the thickness of the white separator in the label
            int white_bar_thickness = 0;
            for (int y = y_topleft + number_height; y < y_botright; y++)
            {
                Color current = bitmap.GetPixel(x_topleft, y);
                if (current != VoltorbFlipColors._WHITE) { break; }
                white_bar_thickness++;
            }
            int lower_y_offset = number_height + white_bar_thickness;

            // NOTE: numbers are not squished between 2 white bounds here, so also gauge the start and end Y-coordinate
            stop = false;
            black_started = false;
            bool reading_number = false; // change between voltorb - number each time a black region is matched
            int black_start_y = -1;
            int black_end_y = -1;
            for (int x = x_topleft; x < x_botright; x++)
            {
                bool contains_black = false;
                for (int y = y_topleft + lower_y_offset; y < y_botright; y++)
                {
                    Color current = bitmap.GetPixel(x, y);
                    if (current == VoltorbFlipColors._BLUE || current == VoltorbFlipColors._DARKGREEN) { stop = true; break; }
                    else if (current == VoltorbFlipColors._WHITE) { break; }
                    else if (current == VoltorbFlipColors._BLACK)
                    {
                        contains_black = true;
                        if (!black_started) { black_started = true; black_start_x = x; }
                        if (reading_number)
                        {
                            if (y > black_end_y) { black_end_y = y; }
                            if (y < black_start_y || black_start_y == -1) { black_start_y = y; }
                        }
                    }
                }
                if (stop) { break; }
                if (!contains_black && black_started)
                {
                    black_started = false;
                    if (reading_number)
                    {
                        black_started = false;
                        int num = NumberDetection.ExtractNumberFromBitmap(bitmap, black_start_x, x, black_start_y, black_end_y + 1);
                        numbers_extracted.Add(num);
                    }
                    reading_number = !reading_number; // switch read mode
                }
            }
            return numbers_extracted;
        }

        private (int, int) GetLastBoxesRow(Bitmap bitmap, int x_topleft, int x_botright, int y_topleft, int y_botright)
        {
            int[] last_jump_left = { x_botright, y_botright };
            int prev_x = x_botright;
            for (int y = y_topleft; y < y_botright; y++)
            {
                for (int x = x_topleft; x < x_botright; x++)
                {
                    Color current = bitmap.GetPixel(x, y);
                    if (current == VoltorbFlipColors._WHITE)
                    {
                        if (x < prev_x) { last_jump_left[0] = x; last_jump_left[1] = y; }
                        prev_x = x;
                        break;
                    }
                }
            }
            // once we have the topleft of the last box, parse left to right
            // until a color is reached that is not White
            // If that color is BackgroundGreen, skip that row
            // Return the first coordinate where this happens
            for (int y = last_jump_left[1]; y < y_botright; y++)
            {
                for (int x = last_jump_left[0]; x < x_botright; x++)
                {
                    Color current = bitmap.GetPixel(x, y);
                    if (current == VoltorbFlipColors._WHITE)
                    {
                        continue;
                    }
                    else if (current == VoltorbFlipColors._LIGHTGREEN)
                    {
                        break;
                    }
                    else
                    {
                        return (x, y);
                    }
                }
            }
            // not used, but useful for error checking
            return (-1, -1);
        }

        private (int, int, List<int>) GetLastBoxesCol(Bitmap bitmap, int x_topleft, int x_botright, int y_topleft, int y_botright)
        {
            int[] last_jump_up = { x_botright, y_botright };
            int prev_y = y_botright;
            for (int x = x_topleft; x < x_botright; x++)
            {
                for (int y = y_topleft; y < y_botright; y++)
                {
                    Color current = bitmap.GetPixel(x, y);
                    if (current == VoltorbFlipColors._WHITE)
                    {
                        if (y < prev_y) { last_jump_up[0] = x; last_jump_up[1] = y; }
                        prev_y = y;
                        break;
                    }
                }
            }
            // once we have the topleft of the last box, parse left to right
            // until a color is reached that is not White
            // If that color is BackgroundGreen, skip that row
            // Find the first coordinate where this happens
            var topleft = (-1, -1);
            for (int y = last_jump_up[1]; y < y_botright; y++)
            {
                for (int x = last_jump_up[0]; x < x_botright; x++)
                {
                    Color current = bitmap.GetPixel(x, y);
                    if (current == VoltorbFlipColors._WHITE)
                    {
                        continue;
                    }
                    else if (current == VoltorbFlipColors._LIGHTGREEN)
                    {
                        break;
                    }
                    else
                    {
                        topleft = (x, y);
                        break;
                    }
                }
                if (topleft != (-1, -1)) { break; }
            }

            // To know the offset to the topleft of the box below the current,
            // count for the 7th color transition
            List<int> y_offsets = new List<int>();
            int prev_y_topleft = topleft.Item2;
            Color prev = bitmap.GetPixel(topleft.Item1, topleft.Item2);
            int n_transitions = 0;
            for (int y = prev_y_topleft + 1; y < y_botright; y++)
            {
                Color current = bitmap.GetPixel(topleft.Item1, y);
                if (current != prev)
                {
                    n_transitions++;
                    if (n_transitions == 7)
                    {
                        n_transitions = 0; // reset
                        y_offsets.Add(y - prev_y_topleft);
                        if (y_offsets.Count == 4) { break; } // only need 4 offsets
                    }
                }
                prev = current;
            }
            return (topleft.Item1, topleft.Item2, y_offsets);
        }

        private void ReadGridContent(bool update)
        {
            // refresh grid solution with new information from grid
            if (this.content == null) { return; }

            // markers are ordered in column-major order
            var (topleft, botright) = this.region_markers;
            List<(int, int)> flipped_boxes_original = AllMarkerPositionsBetween(this.content, VoltorbFlipColors.FLIPPED_TOPLEFT, topleft.Item1, botright.Item1, topleft.Item2, botright.Item2);
            List<(int, int)> unflipped_boxes_unfiltered = AllMarkerPositionsBetween(this.content, VoltorbFlipColors.UNFLIPPED_BOX, topleft.Item1, botright.Item1, topleft.Item2, botright.Item2);

            List<(int, int, bool)> boxes = new List<(int, int, bool)>();  // bool: is this box flipped?
            if (flipped_boxes_original.Count > 0)
            {
                // move flipped boxes topleft coordinate (+1,+1)
                // The marker is 2x2 and we are interested in the bottom right
                List<(int, int)> flipped_boxes = new List<(int, int)>();
                foreach (var coord in flipped_boxes_original)
                {
                    flipped_boxes.Add((coord.Item1 + 1, coord.Item2 + 1));
                }

                // merge box lists into one
                int runlength = 0;
                int prev_x = -1;
                int flipped_index = 0;
                for (int unfiltered_index = 0; unfiltered_index <= unflipped_boxes_unfiltered.Count; unfiltered_index++)
                {
                    if (unflipped_boxes_unfiltered.Count == 0) { break; } // all squared flipped: postgame screen
                    var coord = unfiltered_index < unflipped_boxes_unfiltered.Count ? unflipped_boxes_unfiltered[unfiltered_index] : (-1, -1);
                    if (unfiltered_index != unflipped_boxes_unfiltered.Count && (prev_x == -1 || prev_x == coord.Item1))
                    {
                        // start, or continue in same column
                        runlength++;
                        boxes.Add((coord.Item1, coord.Item2, false));
                        prev_x = coord.Item1;  // useless, but harmless if prev_x != 1
                    }
                    else
                    {
                        // check for previous column being all flipped
                        if (flipped_index < flipped_boxes.Count && flipped_boxes[flipped_index].Item1 < boxes[boxes.Count - 1].Item1)
                        {
                            for (int i = flipped_index; i < flipped_boxes.Count; i++)
                            {
                                var box = flipped_boxes[i];
                                if (box.Item1 < boxes[boxes.Count - 1].Item1)
                                {
                                    boxes.Insert(boxes.Count - runlength, (box.Item1, box.Item2, true));
                                    flipped_index++;
                                }
                            }
                        }
                        // new column, check runlength
                        for (int _ = runlength; _ < 5; _++)
                        {
                            bool inserted = false;
                            for (int i = boxes.Count - runlength; i < boxes.Count; i++)
                            {
                                if (flipped_boxes[flipped_index].Item1 < boxes[i].Item1 || flipped_boxes[flipped_index].Item2 < boxes[i].Item2)
                                {
                                    var box = flipped_boxes[flipped_index];
                                    boxes.Insert(i, (box.Item1, box.Item2, true));
                                    inserted = true;
                                    flipped_index++;
                                    break;
                                }
                            }
                            if (!inserted)
                            {
                                var box = flipped_boxes[flipped_index];
                                boxes.Add((box.Item1, box.Item2, true));
                                flipped_index++;
                            }
                        }
                        if (unfiltered_index == unflipped_boxes_unfiltered.Count) { break; }
                        runlength = 1;
                        boxes.Add((coord.Item1, coord.Item2, false));
                        prev_x = coord.Item1;  // new column started
                    }
                }
                // finish last flipped boxes behind the last unflipped box
                for (int i = flipped_index; i < flipped_boxes.Count; i++)
                {
                    var new_box = flipped_boxes[i];
                    boxes.Add((new_box.Item1, new_box.Item2, true));
                }
            }else
            {
                if (unflipped_boxes_unfiltered.Count == 24)
                {
                    // red selection region around unflipped box indicates new round, so add dummy box
                    boxes.Add((0, 0, false));
                }
                for (int i=0;i< unflipped_boxes_unfiltered.Count; i++)
                {
                    var new_box = unflipped_boxes_unfiltered[i];
                    boxes.Add((new_box.Item1, new_box.Item2, false));
                }
            }

            for (int i = 0; i < 5; i++)
            {
                for (int j = 0; j < 5; j++)
                {
                    var box = boxes[j * 5 + i];
                    if (box.Item3)
                    {
                        int num = ReadGridBox(box.Item1, box.Item2);
                        this.grid.Set(i, j, num);
                    }
                }
            }
            if (update) { UpdateGridDisplay(); }// recalculate safe positions from new information
        }

        // Reads number from a box in the grid
        private int ReadGridBox(int x_topleft, int y_topleft)
        {
            bool stop = false;
            bool black_started = false;
            int black_start_x = -1;
            int black_start_y = -1;
            int black_end_y = -1;
            for (int x = x_topleft; x < this.content.Width; x++)
            {
                bool contains_black = false;
                for (int y = y_topleft; y < this.content.Height; y++)
                {
                    Color current = this.content.GetPixel(x, y);
                    if (current == VoltorbFlipColors._LIGHTGREEN) { stop = true; break; }
                    else if (current == VoltorbFlipColors._BROWN) { break; }
                    else if (current == VoltorbFlipColors._BLACK)
                    {
                        contains_black = true;
                        if (!black_started) { black_started = true; black_start_x = x; }
                        if (y > black_end_y) { black_end_y = y; }
                        if (y < black_start_y || black_start_y == -1) { black_start_y = y; }
                    }
                }
                if (stop) { break; }
                if (!contains_black && black_started)
                {
                    int num = NumberDetection.ExtractNumberFromBitmap(this.content, black_start_x, x, black_start_y, black_end_y + 1);
                    //this.richTextBox1.AppendText("found a number: " + num + "\n");
                    return num;
                }
            }
            return -1;
        }
    }
}
