﻿//MIT, 2017-present, WinterDev
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using PixelFarm.TreeCollection;
using PaintLab.DocumentPro;

using LayoutFarm.TextEditing;

namespace Test_TreeCollection
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            //test segment tree, 
            //with overlapped segment ...
            TreeSegment t1 = new TreeSegment(0, 10);
            TreeSegment t2 = new TreeSegment(8, 20);
            SegmentTree<TreeSegment> tree1 = new SegmentTree<TreeSegment>();
            tree1.Add(t1);
            tree1.Add(t2);
            foreach (var seg in tree1.GetSegmentsAt(9))
            {

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            RedBlackTreeTests tests = new RedBlackTreeTests();
            tests.TestAddBug();
            tests.TestRemoveBug();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            HeightTests heightTest = new HeightTests();
            heightTest.Setup();
            heightTest.TestHeightChanged();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //string text = "hello\r\nIts me!";
            string text = "123456\r\nIts me!\r\n";
            TextSource textsource = new TextSource(text.ToCharArray());
            char c = textsource.GetCharAt(1, 3);
            if (c != '3')
            {

            }
            //the text source is immutable!
            //if we want to make a change 
            //just create a new version of that
        }

        void AnotherTextDocumentTest()
        {

        }

        private void button5_Click(object sender, EventArgs e)
        {
            PlainTextDocument document = new PlainTextDocument();
            for (int i = 0; i < 10; ++i)
            {
                document.AppendLine(i.ToString());
            }

            //test...
            PlainTextLine textline1 = document.GetLine(1);
            document.Insert(0, "x");
            PlainTextLine textline2 = document.GetLine(2);
            document.Remove(0); 

            StringBuilder stbuilder = new StringBuilder();
            document.CopyAllText(stbuilder);


            PlainTextDocument doc2 = document.Clone();
            stbuilder.Length = 0;
            doc2.CopyAllText(stbuilder);
        }
    }
}
