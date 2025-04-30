using System.Globalization;
using Lab5FR.Models;
using static System.Reflection.Metadata.BlobBuilder;

namespace Lab5FR.Services
{
    public class LibraryService
    {
        // All books in the system remain in allBooks.
        private List<Book> allBooks = new();
        // Books available for checkout.
        private List<Book> availableBooks = new();
        private List<User> users = new();
        private Dictionary<int, List<Book>> borrowedBooks = new();

        public LibraryService()
        {
            ReadBooks();
            ReadUsers();
        }

        public List<Book> GetAllBooks() => allBooks;

        public List<Book> GetAvailableBooks() => availableBooks;

        public bool IsBookAvailable(int bookId) => availableBooks.Any(b => b.Id == bookId);


//Testing
        public LibraryService(bool skipFileLoading)
        {
            if (!skipFileLoading)
            {
                ReadBooks();
                ReadUsers();
            }
        }

        private bool enableFileWriting = true;

        public void DisableFileWritingForTests() => enableFileWriting = false;

//End of Testing 
        private void ReadBooks()
        {
            try
            {
                string path = Path.Combine("wwwroot", "Data", "Books.csv");

                if (!File.Exists(path))
                    return;

                foreach (var line in File.ReadLines(path))
                {
                    var fields = line.Split(',');

                    if (fields.Length >= 4)
                    {
                        string idStr = fields[0];
                        string isbn = fields[^1];
                        string author = fields[^2];
                        string title = string.Join(",", fields.Skip(1).Take(fields.Length - 3));

                        var book = new Book
                        {
                            Id = int.Parse(idStr.Trim(), CultureInfo.InvariantCulture),
                            Title = title.Trim('"').Trim(),
                            Author = author.Trim(),
                            ISBN = isbn.Trim()
                        };

                        // Add to both the complete collection and available list.
                        allBooks.Add(book);
                        availableBooks.Add(book);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LibraryService] Error reading books: {ex.Message}");
            }
        }

        private void ReadUsers()
        {
            try
            {
                string path = Path.Combine("wwwroot", "Data", "Users.csv");

                if (!File.Exists(path))
                    return;

                foreach (var line in File.ReadLines(path))
                {
                    var fields = line.Split(',');

                    if (fields.Length >= 3)
                    {
                        var user = new User
                        {
                            Id = int.Parse(fields[0].Trim()),
                            Name = fields[1].Trim(),
                            Email = fields[2].Trim()
                        };

                        users.Add(user);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[LibraryService] Error reading users: {ex.Message}");
            }
        }

        public void AddBook(Book newBook)
        {
            newBook.Id = allBooks.Any() ? allBooks.Max(b => b.Id) + 1 : 1;
            allBooks.Add(newBook);
            availableBooks.Add(newBook);
            WriteBooksToFile();
        }

        private void WriteBooksToFile()
        {
            if (!enableFileWriting) return;
            string path = Path.Combine("wwwroot", "Data", "Books.csv");
            var lines = allBooks.Select(b => $"{b.Id},{Escape(b.Title)},{Escape(b.Author)},{b.ISBN}");
            File.WriteAllLines(path, lines);
        }

        private string Escape(string value) => value.Contains(',') ? $"\"{value}\"" : value;

        public void AddUser(User newUser)
        {
            newUser.Id = users.Any() ? users.Max(u => u.Id) + 1 : 1;
            users.Add(newUser);
            WriteUsersToFile();
        }

        public void UpdateUser(User updatedUser)
        {
            var existing = users.FirstOrDefault(u => u.Id == updatedUser.Id);
            if (existing != null)
            {
                existing.Name = updatedUser.Name;
                existing.Email = updatedUser.Email;
                WriteUsersToFile();
            }
        }


        public List<User> GetUsers()
        {
            return users;
        }

        private void WriteUsersToFile()
        {
            if (!enableFileWriting) return;
            string path = Path.Combine("wwwroot", "Data", "Users.csv");
            var lines = users.Select(u => $"{u.Id},{Escape(u.Name)},{u.Email}");
            File.WriteAllLines(path, lines);
        }

        public void DeleteBook(int bookId)
        {
            var book = allBooks.FirstOrDefault(b => b.Id == bookId);
            if (book != null)
            {
                allBooks.Remove(book);
                availableBooks.Remove(book);
                WriteBooksToFile();
            }
        }

        public void UpdateBook(Book updatedBook)
        {
            var index = allBooks.FindIndex(b => b.Id == updatedBook.Id);
            if (index != -1)
            {
                allBooks[index] = updatedBook;
                WriteBooksToFile();
            }
        }

        public void DeleteUser(int userId)
        {
            var user = users.FirstOrDefault(u => u.Id == userId);
            if (user != null)
            {
                users.Remove(user);
                WriteUsersToFile();
            }
        }

        public bool BorrowBook(int userId, int bookId)
        {
            var user = users.FirstOrDefault(u => u.Id == userId);
            var book = availableBooks.FirstOrDefault(b => b.Id == bookId);

            if (user == null || book == null)
                return false;

            // Remove book from available list.
            availableBooks.Remove(book);

            // Add to user's borrowed list.
            if (!borrowedBooks.ContainsKey(userId))
                borrowedBooks[userId] = new List<Book>();

            borrowedBooks[userId].Add(book);

            WriteBooksToFile(); // Updates CSV to reflect availability.
            return true;
        }


        public bool ReturnBook(int userId, int bookId)
        {
            if (!borrowedBooks.ContainsKey(userId))
                return false;

            var borrowedList = borrowedBooks[userId];
            var bookToReturn = borrowedList.FirstOrDefault(b => b.Id == bookId);

            if (bookToReturn == null)
                return false;

            borrowedList.Remove(bookToReturn);
            availableBooks.Add(bookToReturn);

            WriteBooksToFile(); // Book is now available again.
            return true;
        }

        public List<Book> GetBorrowedBooksByUser(int userId)
        {
            return borrowedBooks.ContainsKey(userId)
                ? borrowedBooks[userId]
                : new List<Book>();
        }

        public List<(Book, User)> GetAllBorrowedBooks()
        {
            var borrowedList = new List<(Book, User)>();

            foreach (var kvp in borrowedBooks)
            {
                var user = users.FirstOrDefault(u => u.Id == kvp.Key);
                if (user != null)
                {
                    foreach (var book in kvp.Value)
                    {
                        borrowedList.Add((book, user));
                    }
                }
            }

            return borrowedList;
        }


    }
}
