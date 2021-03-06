﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
#pragma warning disable 0108

///////////////////////////////////////////////////////////////////
//
//      Modifié par Xavier Brosseau et Melissa Boucher
//
///////////////////////////////////////////////////////////////////

namespace Compact_Agenda
{
    public partial class Form_WeekView : Form
    {
        public string ConnexionString;
        private DateTime _CurrentWeek;
        private Events Events = new Events();
        Point lastMouseLocation;
        Point firstMouseLocation;
        bool mouseIsDown = false;
        Pen pen = new Pen(Color.Orange, 2);
        private int minInterval = 5;
        public bool delete = false;

        public DateTime CurrentWeek
        {
            set
            {
                // calculer la date du dimanche de la semaine courante
                _CurrentWeek = value.AddDays(-(int)value.DayOfWeek);
            }
            get { return _CurrentWeek; }
        }

        public Form_WeekView()
        {
            InitializeComponent();
            // Ici on assume que la BD est dans le même répertoire que l'éxécutable
            // faire attention de copier la bd dans le répertoire release et/ou debug selon le cas
            string DB_URL = System.IO.Path.GetFullPath(@"DB_AGENDA.MDF");
            ConnexionString = @"Data Source=(LocalDB)\v11.0;AttachDbFilename='" + DB_URL + "';Integrated Security=True";
            CurrentWeek = DateTime.Now;
            PN_Hours.Height = PN_Content.Height = 2400;
            this.UCS_HauteurCase.Maximum = 36;
            this.UCS_HauteurCase.Minimum = 2;
            this.UCS_HauteurCase.Value = 12;
            this.Size = Properties.Settings.Default.SizeWeekView;
            this.Location = Properties.Settings.Default.PositionWeekView;
            //Color            
            LoadEverything();

        }

        // Couleur du thème de base 
        private void DefaultColorWeekView()
        {
            Font fontGeneral = new Font("Lucida Console", 8, FontStyle.Regular, GraphicsUnit.Point);
            Font fontEvent = new Font("Papyrus", 9, FontStyle.Regular, GraphicsUnit.Point);
            Properties.Settings.Default.DateFont = fontGeneral;
            Properties.Settings.Default.MainFont = fontEvent;
            Properties.Settings.Default.ColorWeekViewTop = Color.FromArgb(238, 207, 83);
            Properties.Settings.Default.ColorWeekViewMain = Color.FromArgb(238, 238, 238);
            Properties.Settings.Default.ColorWeekViewBackG = Color.FromArgb(247, 247, 176);
            Properties.Settings.Default.ColorLignehorizontale = Color.Black;
            Properties.Settings.Default.ColorLigneVerticale = Color.Black;
            Properties.Settings.Default.ColorFont = Color.Black; 
            Properties.Settings.Default.Save();
            RefreshAllAndEverything();
        }

        // Load tout les settings dans les parametres necessaires
        private void LoadEverything()
        {
            PN_Frame.BackColor = Properties.Settings.Default.ColorWeekViewTop;
            PN_DaysHeader.BackColor = Properties.Settings.Default.ColorWeekViewTop;
            PN_Hours.BackColor = Properties.Settings.Default.ColorWeekViewBackG;
            PN_Scroll.BackColor = Properties.Settings.Default.ColorWeekViewBackG;
            PN_Content.BackColor = Properties.Settings.Default.ColorWeekViewMain;
            PN_Hours.Font = Properties.Settings.Default.DateFont;
            PN_DaysHeader.Font = Properties.Settings.Default.DateFont;
            PN_DaysHeader.ForeColor = Properties.Settings.Default.ColorFont;
            PN_Hours.ForeColor = Properties.Settings.Default.ColorFont;
            PN_Frame.ForeColor = Properties.Settings.Default.ColorFont;
            PN_Scroll.ForeColor = Properties.Settings.Default.ColorFont;
            PN_Content.ForeColor = Properties.Settings.Default.ColorFont;
            this.Location = Properties.Settings.Default.PositionWeekView;
            RefreshAllAndEverything();
        }

        // refresh tout les panels
        private void RefreshAllAndEverything()
        {
            PN_Content.Refresh();
            PN_DaysHeader.Refresh();
            PN_Frame.Refresh();
            PN_Hours.Refresh();
            PN_Scroll.Refresh();
            this.Refresh();
        }
        
        private void Form_WeekView_Load(object sender, EventArgs e)
        {
            PN_Scroll.Focus();
            GotoCurrentWeek();  
        }

        private void PN_Scroll_MouseEnter(Object sender, EventArgs e)
        {
            PN_Scroll.Focus();
            UCS_HauteurCase.Visible = false;
        }


        private void GetWeekEvents()
        {
            TableEvents tableEvents = new TableEvents(ConnexionString);
            DateTime date = _CurrentWeek;
            Events.Clear();
            for (int day = 0; day < 7; day++)
            {
                tableEvents.GetDateEvents(date);
                while (tableEvents.NextEventRecord())
                {
                    tableEvents.currentEventRecord.ParentPanel = PN_Content;
                    Events.Add(tableEvents.currentEventRecord);
                }
                tableEvents.EndQuerySQL();
                date = date.AddDays(1);
            }
        }

        private void Fill_Agenda(Graphics DC)
        {
            Brush brush = new SolidBrush(Color.Black);
            Pen pen1 = new Pen(Properties.Settings.Default.ColorLignehorizontale, 1);
            Pen pen2 = new Pen(Properties.Settings.Default.ColorLignehorizontale, 1);
            Pen pen3 = new Pen(Properties.Settings.Default.ColorLigneVerticale, 1);
            pen2.DashStyle = System.Drawing.Drawing2D.DashStyle.Dot;
            for (int hour = 0; hour < 24; hour++)
            {
                DC.DrawLine(pen1, 0, Event.HourToPixel(hour + 1, 0, PN_Hours.Height), PN_Content.Width, Event.HourToPixel(hour + 1, 0, PN_Hours.Height));
                DC.DrawLine(pen2, 0, Event.HourToPixel(hour + 1, 30, PN_Hours.Height), PN_Content.Width, Event.HourToPixel(hour + 1, 30, PN_Hours.Height));
            }
            Point location;
            for (int dayNum = 0; dayNum < 7; dayNum++)
            {
                location = new Point((int)Math.Round(PN_DaysHeader.Width / 7f * dayNum), 0);
                DC.DrawLine(pen3, location.X, 0, location.X, PN_Content.Height);
            }
            location = new Point((int)Math.Round(PN_DaysHeader.Width / 7f * 7), 0);
            DC.DrawLine(pen3, location.X - 1, 0, location.X - 1, PN_Content.Height);
            Events.Draw(DC);
            PN_Scroll.Focus();
        }

        private void PN_Content_Paint(object sender, PaintEventArgs e)
        {
            Fill_Agenda(e.Graphics);
        }

        private void Fill_Days_Header(Graphics DC)
        {
            Point location;
            DateTime date = _CurrentWeek;
            string[] dayNames = System.Globalization.CultureInfo.CurrentCulture.DateTimeFormat.DayNames;
            Brush brush = new SolidBrush( Properties.Settings.Default.ColorFont);
            Pen pen = new Pen(Color.Gray, 1);
            for (int dayNum = 0; dayNum < 7; dayNum++)
            {
                location = new Point((int)Math.Round(PN_DaysHeader.Width / 7f * dayNum), 0);
                // colore la journée courante 
                if (DateTime.Parse(date.Date.ToShortDateString()) == DateTime.Parse(DateTime.Now.Date.ToShortDateString()))
                {
                    Rectangle border = new Rectangle(location.X, location.Y, (int)Math.Round(PN_DaysHeader.Width / 7f * (dayNum + 1)) - location.X, PN_DaysHeader.Height);
                    using (Brush B = new SolidBrush(Color.Orange))
                    {
                        DC.FillRectangle(B, border);
                    }
                }
                String headerText = dayNames[dayNum];
                String headerDate = date.ToShortDateString();
                DC.DrawLine(pen, location.X, 0, location.X, PN_DaysHeader.Height);
                DC.DrawString(headerText, PN_DaysHeader.Font, brush, location);
                DC.DrawString(headerDate, PN_DaysHeader.Font, brush, location.X, location.Y + 14);
                date = date.AddDays(1);
            }
            location = new Point((int)Math.Round(PN_DaysHeader.Width / 7f * 7), 0);
            DC.DrawLine(pen, location.X - 1, 0, location.X - 1, PN_DaysHeader.Height);
        }

        private void Fill_Hours_Header(Graphics DC)
        {
            Brush brush = new SolidBrush( Properties.Settings.Default.ColorFont);
            Pen pen = new Pen(Color.LightGray, 1);
            for (int hour = 0; hour <= 24; hour++)
            {
                Point location = new Point(0, Event.HourToPixel(hour, 0, PN_Hours.Height));
                // colore l'heure courante
                if (DateTime.Now.Hour == hour)
                {
                    Rectangle border = new Rectangle(location.X, location.Y, PN_Hours.Width, Event.HourToPixel(hour + 1, 0, PN_Hours.Height) - location.Y);
                    using (Brush B = new SolidBrush(Color.Orange))
                    {
                        DC.FillRectangle(B, border);
                    }
                }
                String headerText = (hour < 10 ? "0" : "") + hour.ToString() + ":00";
                DC.DrawString(headerText, PN_DaysHeader.Font, brush, location);
                DC.DrawLine(pen, 0, Event.HourToPixel(hour + 1, 0, PN_Hours.Height), PN_Hours.Width, Event.HourToPixel(hour + 1, 0, PN_Hours.Height));
            }
        }

        private void PN_DaysHeader_Paint(object sender, PaintEventArgs e)
        {
            Fill_Days_Header(e.Graphics);
        }

        private void PN_Hours_Paint(object sender, PaintEventArgs e)
        {
            Fill_Hours_Header(e.Graphics);
        }

        private void AdjustMinInterval()
        {
            minInterval = 5;
            while (Event.HourToPixel(0, minInterval, PN_Content.Height) < 5)
                minInterval += 5;
        }
        private void PN_Scroll_Resize(object sender, EventArgs e)
        {
            PN_Content.Width = PN_Scroll.Width - 70;
            PN_DaysHeader.Width = PN_Content.Width;
            PN_DaysHeader.Width = PN_Content.Width;
            PN_DaysHeader.Refresh();
            PN_Content.Refresh();
        }

        private void PN_Scroll_Scroll(object sender, ScrollEventArgs e)
        {
            PN_DaysHeader.Refresh();
            PN_Content.Refresh();
        }

        private int RoundToMinutes(int pixel, int interval)
        {
            int pixel_interval = Event.HourToPixel(0, interval, PN_Content.Height);
            if (pixel_interval > 5)
            {
                int half_interval = (int)Math.Round(pixel_interval / 2f);

                int minutes = Event.PixelToMinutes(pixel + half_interval, PN_Content.Height);
                minutes = (int)Math.Truncate((float)minutes / interval) * interval;
                int hour = (int)Math.Truncate(minutes / 60f);
                minutes = minutes - hour * 60;
                return Event.HourToPixel(hour, minutes, PN_Content.Height);
            }
            else
                return pixel;
        }
        private void PN_Content_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right)
            {
                mouseIsDown = true;
            }
            firstMouseLocation = lastMouseLocation = e.Location;
            if (Events.TargetEvent != null)
            {
                switch (Events.TargetPart)
                {
                    case TargetPart.Top:
                        firstMouseLocation.Y =
                        lastMouseLocation.Y = RoundToMinutes(Event.HourToPixel(Events.TargetEvent.Starting.Hour, Events.TargetEvent.Starting.Minute, PN_Content.Height), minInterval);
                        break;
                    case TargetPart.Bottom:
                        firstMouseLocation.Y =
                        lastMouseLocation.Y = RoundToMinutes(Event.HourToPixel(Events.TargetEvent.Ending.Hour, Events.TargetEvent.Ending.Minute, PN_Content.Height), minInterval);
                        break;
                    default: break;
                }
            }
        }

        private void AjustCurrentWeek()
        {
            DateTime Target = new DateTime(Events.TargetEvent.Starting.Year, Events.TargetEvent.Starting.Month, Events.TargetEvent.Starting.Day, 0, 0, 0);
            DateTime CW = new DateTime(_CurrentWeek.Year, _CurrentWeek.Month, _CurrentWeek.Day, 0, 0, 0);
            int delta = (int)(Target - CW).TotalDays;
            if (delta > 6)
            {
                Event currentTarget = Events.TargetEvent.Klone();
                Increment_Week();
                Events.Add(currentTarget);
                currentTarget.Draw(PN_Content.CreateGraphics());
                Events.TargetEvent = currentTarget;
                Cursor.Position = new Point(Cursor.Position.X - 7 * (int)(PN_Content.Width / 7F), Cursor.Position.Y);
            }
            if (delta < 0)
            {
                Event currentTarget = Events.TargetEvent.Klone();
                Decrement_Week();
                Events.Add(currentTarget);
                currentTarget.Draw(PN_Content.CreateGraphics());
                Events.TargetEvent = currentTarget;
                Cursor.Position = new Point(Cursor.Position.X + 7 * (int)(PN_Content.Width / 7F), Cursor.Position.Y);
            }
        }

        private void AdjustScroll(int value)
        {
            int PN_Scroll_Mouse_Location = value - PN_Scroll.VerticalScroll.Value;
            int hour_heigth = Event.HourToPixel(1, 0, PN_Scroll.Height);

            if (PN_Scroll_Mouse_Location < 0)
            {
                Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y + hour_heigth);
                if ((PN_Scroll.VerticalScroll.Value - hour_heigth) > hour_heigth)
                    PN_Scroll.VerticalScroll.Value -= hour_heigth;
            }
            if (PN_Scroll_Mouse_Location > PN_Scroll.Height)
            {
                Cursor.Position = new Point(Cursor.Position.X, Cursor.Position.Y - hour_heigth);
                if ((PN_Scroll.VerticalScroll.Value + hour_heigth) < PN_Content.Height)
                    PN_Scroll.VerticalScroll.Value += hour_heigth;
            }
        }

        private static DateTime Klone(DateTime x)
        {
            return new DateTime(x.Year, x.Month, x.Day, x.Hour, x.Minute, 0);
        }

        private void PN_Content_MouseMove(object sender, MouseEventArgs e)
        {
            int Bottom = RoundToMinutes(e.Location.Y, minInterval);
            if (mouseIsDown)
            {
                AdjustScroll(e.Location.Y);
                if (Events.TargetEvent != null)
                {
                    DateTime Moving = LocationToDateTime(new Point(RoundToMinutes(firstMouseLocation.X, minInterval), Bottom));
                    switch (Events.TargetPart)
                    {
                        case TargetPart.Top:
                            if (Moving > Events.TargetEvent.Ending)
                            {
                                Events.TargetPart = TargetPart.Bottom;
                                Events.TargetEvent.Starting = Klone(Events.TargetEvent.Ending);
                                Events.TargetEvent.Ending = Moving;
                            }
                            else
                                Events.TargetEvent.Starting = Moving;
                            break;
                        case TargetPart.Bottom:

                            if (Moving < Events.TargetEvent.Starting)
                            {
                                Events.TargetPart = TargetPart.Top;
                                Events.TargetEvent.Ending = Klone(Events.TargetEvent.Starting);
                                Events.TargetEvent.Starting = Moving;
                            }
                            else
                                Events.TargetEvent.Ending = Moving;
                            break;
                        case TargetPart.Inside:
                            int deltaY = RoundToMinutes(e.Location.Y, minInterval) - RoundToMinutes(lastMouseLocation.Y, minInterval);
                            Events.TargetEvent.Starting = LocationToDateTime(new Point(e.Location.X, RoundToMinutes(Event.HourToPixel(Events.TargetEvent.Starting.Hour, Events.TargetEvent.Starting.Minute, PN_Content.Height) + deltaY, minInterval)));
                            Events.TargetEvent.Ending = LocationToDateTime(new Point(e.Location.X, RoundToMinutes(Event.HourToPixel(Events.TargetEvent.Ending.Hour, Events.TargetEvent.Ending.Minute, PN_Content.Height) + deltaY, minInterval)));
                            AjustCurrentWeek();
                            break;
                        default: break;
                    }
                    PN_Content.Refresh();
                }
                else
                {
                    int day = (int)Math.Truncate(firstMouseLocation.X / (PN_Content.Width / 7F));
                    PN_Content.Cursor = Cursors.Cross;
                    Point top = new Point((int)Math.Round(day * PN_Content.Width / 7F), RoundToMinutes(firstMouseLocation.Y, minInterval));
                    Rectangle border = new Rectangle(top.X + 1, (int)Math.Min(top.Y, Bottom), (int)Math.Round(PN_Content.Width / 7F) - 2, (int)Math.Abs(Bottom - top.Y));
                    PN_Content.Refresh();
                    PN_Content.CreateGraphics().DrawRectangle(pen, border);
                }
            }
            else
                Events.UpdateTarget(e.Location);
            lastMouseLocation = e.Location;
        }

        private DateTime LocationToDateTime(Point location)
        {
            DateTime date = new DateTime(_CurrentWeek.Year, _CurrentWeek.Month, _CurrentWeek.Day);
            int adjust = (location.X < 0 ? (int)(PN_Content.Width / 7F) : 0);
            int days = (int)(Math.Truncate((location.X - adjust) / (PN_Content.Width / 7F)));

            date = date.AddDays(days);
            int Minutes = (int)Math.Max(Event.PixelToMinutes(RoundToMinutes(location.Y, minInterval), PN_Content.Height), 0);
            int Hours = (int)Math.Min((int)Math.Truncate(Minutes / 60f), 23);
            Minutes = Minutes - Hours * 60;
            if (Minutes >= 60)
                Minutes = 59;
            return new DateTime(date.Year, date.Month, date.Day, Hours, Minutes, 0);
        }

        private void ConludeMouseEvent()
        {

            if (mouseIsDown)
            {
                mouseIsDown = false;

                if (Events.TargetEvent != null)
                {
                    if (Events.TargetEvent.Starting > Events.TargetEvent.Ending)
                    {
                        Events.TargetPart = TargetPart.Top;
                        DateTime d = Events.TargetEvent.Starting;
                        Events.TargetEvent.Starting = Events.TargetEvent.Ending;
                        Events.TargetEvent.Ending = d;
                    }

                    TimeSpan delta = Events.TargetEvent.Ending.Subtract(Events.TargetEvent.Starting);
                    if (delta.Minutes < 30 && delta.Hours == 0)
                    {
                        Events.TargetEvent.Ending = Events.TargetEvent.Starting + new TimeSpan(0, 30, 0);
                    }
                    TableEvents tableEvents = new TableEvents(ConnexionString);
                    tableEvents.UpdateEventRecord(Events.TargetEvent);
                }
                else
                {
                    DLG_Events dlg = new DLG_Events();
                    Event Event = new Event();

                    Event.Starting = LocationToDateTime(firstMouseLocation);

                    DateTime date = LocationToDateTime(lastMouseLocation);
                    Event.Ending = new DateTime(Event.Starting.Year, Event.Starting.Month, Event.Starting.Day, date.Hour, date.Minute, 0);

                    if (Event.Starting > Event.Ending)
                    {
                        Events.TargetPart = TargetPart.Top;
                        DateTime d = Event.Starting;
                        Event.Starting = Event.Ending;
                        Event.Ending = d;

                    }
                    TimeSpan delta = Event.Ending.Subtract(Event.Starting);
                    if (delta.Minutes < 30 && delta.Hours == 0)
                    {
                        Event.Ending = Event.Starting + new TimeSpan(0, 30, 0);
                    }
                    dlg.Event = Event.Klone();
                    if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        TableEvents tableEvents = new TableEvents(ConnexionString);
                        tableEvents.AddEvent(dlg.Event);
                    }
                }
                GetWeekEvents();
                PN_Content.Refresh();
            }

        }

        private void PN_Content_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
                ConludeMouseEvent();
            else if (e.Button == MouseButtons.Right)
            {
                Events.UpdateTarget(e.Location);
                if (Events.TargetEvent != null)
                    CMS_Evenement.Show(Form_WeekView.MousePosition);
                else
                    CMS_FondSemaineCourante.Show(Form_WeekView.MousePosition);
            }
        }

        private void Decrement_Week()
        {
            _CurrentWeek = _CurrentWeek.AddDays(-7);
            GetWeekEvents();
        }
        private void Increment_Week()
        {
            _CurrentWeek = _CurrentWeek.AddDays(7);
            GetWeekEvents();
        }
        private void Increment_Month()
        {
            int currentMonth = CurrentWeek.Month;
            while (currentMonth == CurrentWeek.Month)
                Decrement_Week();

            PN_Content.Refresh();
            PN_DaysHeader.Refresh();
        }
        private void Decrement_Month()
        {
            int month = CurrentWeek.AddDays(6).Month;
            while (month == CurrentWeek.AddDays(6).Month)
                Increment_Week();

            PN_Content.Refresh();
            PN_DaysHeader.Refresh();
        }

        private void GotoCurrentWeek()
        {
            CurrentWeek = DateTime.Now;
            GetWeekEvents();
            PN_Content.Refresh();
            PN_DaysHeader.Refresh();
            PN_Scroll.VerticalScroll.Value = Event.HourToPixel((int)Math.Max(DateTime.Now.Hour - 3, 0), 0, PN_Hours.Height);
        }

        private void FBTN_DecrementWeek_Click(object sender, EventArgs e)
        {
            Decrement_Week();
            PN_Content.Refresh();
            PN_DaysHeader.Refresh();
        }

        private void FBTN_IncrementWeek_Click(object sender, EventArgs e)
        {
            Increment_Week();
            PN_Content.Refresh();
            PN_DaysHeader.Refresh();
        }

        private void PN_Content_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            AjouterModifierDLG();
        }

        // Methode utilisé pour zoomer 
        private void zoom(int Valeur, int DeCombien = 200)
        {
            PN_Content.Height = Valeur * DeCombien;
            PN_Hours.Height = Valeur * DeCombien;
            PN_Content.Refresh();
            PN_Hours.Refresh();
        }
        // Methode utilisé pour zoomer 
        private void ZoomIn(int Valeur = 200)
        {
            if (!mouseIsDown)
            {
                if (PN_Content.Height < PN_Frame.Height * 12)
                {
                    PN_Content.Height += Valeur;
                    PN_Hours.Height += Valeur;
                    PN_Content.Refresh();
                    PN_Hours.Refresh();
                    UCS_HauteurCase.Value ++;
                }
            }
        }
        // Methode utilisé pour zoomer 
        private void ZoomOut(int Valeur = 200)
        {
            if (!mouseIsDown)
            {
                if (PN_Content.Height > (PN_Frame.Height))
                {
                    PN_Content.Height -= Valeur;
                    PN_Hours.Height -= Valeur;
                    PN_Content.Refresh();
                    PN_Hours.Refresh();
                    UCS_HauteurCase.Value --;
                }
            }
        }

        // Methode utilisé pour checker les keys press
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.OemMinus:
                    // Fonction temporaire pour voir comment dézommer
                    ZoomOut();
                    break;
                case Keys.Right: // Incrémenter d'une semaine la semaine courrante
                    if (!mouseIsDown)
                        Increment_Week();
                    PN_Content.Refresh();
                    PN_DaysHeader.Refresh();
                    break;
                case Keys.Oemplus:
                    // Fonction temporaire pour voir comment zommer
                    ZoomIn();
                    break;
                case Keys.Left: // Décrémenter d'une semaine la se maine courrante
                    if (!mouseIsDown)
                        Decrement_Week();
                    PN_Content.Refresh();
                    PN_DaysHeader.Refresh();
                    break;
                case Keys.Space:
                    if (!mouseIsDown)
                        GotoCurrentWeek();
                    break;
                case Keys.Up:
                    if (!mouseIsDown)
                        Increment_Month();
                    break;
                case Keys.Down:
                    if (!mouseIsDown)
                        Decrement_Month();
                    break;
                case Keys.F1:
                    FenetreInfo();
                    break;
                case (Keys.Control | Keys.Q):
                    this.Close();
                    break;
                case Keys.Escape:
                    this.Close();
                    break;
                case Keys.F3:
                    MettreLaSemaineALaDateChoisi();
                    break;
                case Keys.F4:
                    DefaultColorWeekView();
                    LoadEverything();
                    this.Refresh();
                    break; 
            }
            bool result = base.ProcessCmdKey(ref msg, keyData);
            PN_Scroll.Focus();
            return result;
        }

        // Methode pour changer la date courante a celle choisi
        private void MettreLaSemaineALaDateChoisi()
        {
            ChoixDate dialog = new ChoixDate();
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                _CurrentWeek = Properties.Settings.Default.DateCourante;
                GotoCurrentWeekCustom(Properties.Settings.Default.DateCourante);
            }  
        }
        // aller a la semaine courante (Space)
        private void GotoCurrentWeekCustom(DateTime JourneeChoisi)
        {
            CurrentWeek = JourneeChoisi;
            GetWeekEvents();
            PN_Content.Refresh();
            PN_DaysHeader.Refresh();
            PN_Scroll.VerticalScroll.Value = Event.HourToPixel((int)Math.Max(DateTime.Now.Hour - 3, 0), 0, PN_Hours.Height);
        }

        // ouverture de la fenetre d'information/aide F1
        private void FenetreInfo()
        {
            Aide dlg = new Aide();

            dlg.ShowDialog();
        }

        private void PN_Content_Resize(object sender, EventArgs e)
        {
            AdjustMinInterval();
        }

        private void PN_Hours_MouseEnter(object sender, EventArgs e)
        {
            UCS_HauteurCase.Visible = true;
        }

        private void UCS_HauteurCase_ValueChanged(object sender, EventArgs e)
        {
            zoom(UCS_HauteurCase.Value);
        }

        private void PN_DaysHeader_MouseEnter(object sender, EventArgs e)
        {
            UCS_HauteurCase.Visible = false;
        }

        private void Form_WeekView_FormClosing(object sender, FormClosingEventArgs e)
        {
            Properties.Settings.Default.PositionWeekView = this.Location;
            Properties.Settings.Default.SizeWeekView = this.Size;
            Properties.Settings.Default.Save();
        }

        private void CMS_DateCouranteEnteteJournee_Click(object sender, EventArgs e)
        {
            MettreLaSemaineALaDateChoisi();
        }

        private void CMS_CouleurEnteteJournee_Click(object sender, EventArgs e)
        {
            ChangerCouleur("FondJournee");
        }

        private void CMS_FontEnteteJournee_Click(object sender, EventArgs e)
        {
            ChangerlesPolicesEntete();
        }

        private void CMS_ModifierEvent_Click(object sender, EventArgs e)
        {
            AjouterModifierDLG();
        }

        private void AjouterModifierDLG()
        {
            if ((Events.TargetEvent != null) && (Events.TargetPart == TargetPart.Inside))
            {
                DLG_Events dlg = new DLG_Events();
                dlg.Event = Events.TargetEvent;
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (dlg.delete)
                    {
                        TableEvents tableEvents = new TableEvents(ConnexionString);
                        tableEvents.DeleteEvent(dlg.Event);
                        Events.TargetEvent = null;
                        mouseIsDown = false;
                    }
                    else
                    {
                        TableEvents tableEvents = new TableEvents(ConnexionString);
                        tableEvents.UpdateEventRecord(dlg.Event);
                    }
                    GetWeekEvents();
                    PN_Content.Refresh();
                }
            }
        }

        private void CMS_EffacerEvent_Click(object sender, EventArgs e)
        {
            if (Events.TargetEvent != null)
            {
                if (MessageBox.Show("Voulez vous vraiment effacer cet événement ?", "Effacer?", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation) == System.Windows.Forms.DialogResult.Yes)
                {
                    Events.UpdateTarget(PN_Content.PointToClient(new Point(CMS_Evenement.Left, CMS_Evenement.Top)));
                    TableEvents tableEvents = new TableEvents(ConnexionString);
                    tableEvents.DeleteEvent(Events.TargetEvent);
                    Events.TargetEvent = null;
                    mouseIsDown = false;
                    GetWeekEvents();
                    this.Refresh();
                }
            }
        }

        private void CMS_ReporterEvent_Click(object sender, EventArgs e)
        {
            Events.UpdateTarget(PN_Content.PointToClient(new Point(CMS_Evenement.Left, CMS_Evenement.Top)));
            TableEvents tableEvents = new TableEvents(ConnexionString);
            Event buffer = Events.TargetEvent;
            buffer.Starting = buffer.Starting.AddDays(7);
            buffer.Ending = buffer.Ending.AddDays(7);
            tableEvents.UpdateEventRecord(Events.TargetEvent);
            Events.TargetEvent = null;
            mouseIsDown = false;
            GetWeekEvents();
            this.Refresh();
        }
        // changer les couleurs passé en commentaire
        private void ChangerCouleur(string EndroitCouleur)
        {
            Compact_Agenda.DLG_HLSColorPicker DialogDeCouleur = new Compact_Agenda.DLG_HLSColorPicker();

            DialogDeCouleur.Text = EndroitCouleur;
            
            if(DialogDeCouleur.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                switch(EndroitCouleur)
                {
                    case "LigneVerticale":
                        Properties.Settings.Default.ColorLigneVerticale = DialogDeCouleur.color;
                        break;
                    case "LigneHorizontale":
                        Properties.Settings.Default.ColorLignehorizontale = DialogDeCouleur.color; 
                        break;
                    case "FondEvent":
                        Properties.Settings.Default.ColorWeekViewMain = DialogDeCouleur.color; 
                        break;
                    case "FondHeures":
                        Properties.Settings.Default.ColorWeekViewBackG = DialogDeCouleur.color;
                        break;
                    case "FondJournee":
                        Properties.Settings.Default.ColorWeekViewTop = DialogDeCouleur.color;
                        break;
                    case "CouleurFont":
                        Properties.Settings.Default.ColorFont = DialogDeCouleur.color;
                        break;
                }
                Properties.Settings.Default.Save();
            }
            LoadEverything();
        }
        // changer les polices choisis 
        private void ChangerlesPolicesEntete()
        {
            FontDialog dlg = new FontDialog();

            if(dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.DateFont = dlg.Font;
                Properties.Settings.Default.Save();
            }
            ChangerCouleur("CouleurFont");
            LoadEverything();
        }
        // changer les polices choisis
        private void ChangerlesPolicesEvent()
        {
            FontDialog dlg = new FontDialog();

            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.MainFont = dlg.Font;
                Properties.Settings.Default.Save();
            }
            ChangerCouleur("CouleurFont");
            LoadEverything();
        }

        private void CMS_DupliquerEvent_Click(object sender, EventArgs e)
        {
            DuplicataEvent dlg = new DuplicataEvent();
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                TableEvents tableEvents = new TableEvents(ConnexionString);
                Events.UpdateTarget(PN_Content.PointToClient(new Point(CMS_Evenement.Left, CMS_Evenement.Top)));
                Event clone = Events.TargetEvent.Klone();

                for (int i = 0; i < dlg.GetNbFois(); i++)
                {
                    switch (dlg.GetInterval())
                    {
                        case "Jour":
                            clone.Starting = clone.Starting.AddDays(1);
                            clone.Ending = clone.Ending.AddDays(1);
                            break;
                        case "Semaine":
                            clone.Starting = clone.Starting.AddDays(7);
                            clone.Ending = clone.Ending.AddDays(7);
                            break;
                        case "Mois":
                            clone.Starting = clone.Starting.AddMonths(1);
                            clone.Ending = clone.Ending.AddMonths(1);
                            break;
                        case "Année":
                            clone.Starting = clone.Starting.AddYears(1);
                            clone.Ending = clone.Ending.AddYears(1);
                            break;
                    }
                    tableEvents.AddEvent(clone);

                }
                GetWeekEvents();
                this.Refresh();
            }
        }

        private void CMS_CouleurEnteteHeure_Click(object sender, EventArgs e)
        {
            ChangerCouleur("FondHeures");
        }

        private void CMS_FontEnteteHeure_Click(object sender, EventArgs e)
        {
            ChangerlesPolicesEntete();
        }

        private void CMS_CouleurSemaine_Click(object sender, EventArgs e)
        {
            ChangerCouleur("FondEvent");
        }

        private void CMS_FontSemaine_Click(object sender, EventArgs e)
        {
            ChangerlesPolicesEvent();
        }

        private void CMS_HorizontaleSemaine_Click(object sender, EventArgs e)
        {
            ChangerCouleur("LigneHorizontale");
        }

        private void CMS_VerticalesSemaine_Click(object sender, EventArgs e)
        {
            ChangerCouleur("LigneVerticale");
        }

        private void PN_Content_MouseClick(object sender, MouseEventArgs e)
        {
           
        }

        private void FBTN_DecrementWeek_MouseEnter(object sender, EventArgs e)
        {
            UCS_HauteurCase.Visible = false; 
        }

        private void PN_Scroll_MouseEnter_1(object sender, EventArgs e)
        {
            UCS_HauteurCase.Visible = false; 
        }

        private void PN_Frame_MouseEnter(object sender, EventArgs e)
        {
            UCS_HauteurCase.Visible = false; 
        }

    }
}
