using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;

namespace TestDemo
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            Books = new ObservableDictionary<int, Book>();
            Books.Add(1, new Book() { Name = "Book 1" });
            Books.Add(2, new Book() { Name = "Book 2" });
            Books.Add(3, new Book() { Name = "Book 3" });

            SelectedBookIndex = 2;

            DataContext = this;
        }

        public ObservableDictionary<int, Book> Books { get; set; }

        private int _selectedBookIndex;
        public int SelectedBookIndex
        {
            get { return _selectedBookIndex; }
            set
            {
                _selectedBookIndex = value;
                Debug.WriteLine("Selected Book Index=" + _selectedBookIndex);
            }
        }

        private void buttonChangeSomething_Click(object sender, RoutedEventArgs e)
        {
            //Books.Remove(1);
            //Books.Add(1, new Book() { Name = "Hello Betty" });

            for (int i = 0; i < Books.Count; i++)
                Books[i + 1] = new Book() { Name = Books[i + 1].Name + "!" };
        }
    }

    public class Book
    {
        public string Name { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
}