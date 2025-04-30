using Microsoft.VisualStudio.TestTools.UnitTesting;
using Lab5FR.Services;
using Lab5FR.Models;
using System.Linq;

namespace LibrarryApp.Tests
{
    [TestClass]
    public class LibraryServiceTests
    {
        private LibraryService service;

        [TestInitialize]
        public void Setup()
        {
            service = new LibraryService(true); // skips file I/O

            // Optional: clear collections just in case
            service.GetAllBooks().Clear();
            service.GetAvailableBooks().Clear();
            service.GetUsers().Clear();
            service = new LibraryService(true);
            service.DisableFileWritingForTests();
        }

        [TestMethod]
        public void AddBook_ShouldAddToAllAndAvailableLists()
        {
            var book = new Book { Title = "1984", Author = "Orwell", ISBN = "1234567890" };

            service.AddBook(book);
            var allBooks = service.GetAllBooks();
            var availableBooks = service.GetAvailableBooks();

            Assert.IsTrue(allBooks.Any(b => b.Title == "1984"));
            Assert.IsTrue(availableBooks.Any(b => b.Title == "1984"));
        }

        [TestMethod]
        public void DeleteBook_ShouldRemoveFromLists()
        {
            var book = new Book { Title = "Test", Author = "Author", ISBN = "000" };
            service.AddBook(book);
            int id = service.GetAllBooks().First().Id;

            service.DeleteBook(id);
            Assert.IsFalse(service.GetAllBooks().Any(b => b.Id == id));
            Assert.IsFalse(service.GetAvailableBooks().Any(b => b.Id == id));
        }

        [TestMethod]
        public void BorrowBook_ShouldRemoveFromAvailableAndTrackAsBorrowed()
        {
            var book = new Book { Title = "Brave New World", Author = "Huxley", ISBN = "999" };
            var user = new User { Name = "Tyler", Email = "tyler@test.com" };

            service.AddBook(book);
            service.AddUser(user);

            int bookId = service.GetAllBooks().First().Id;
            int userId = service.GetUsers().First().Id;

            bool success = service.BorrowBook(userId, bookId);

            Assert.IsTrue(success);
            Assert.IsFalse(service.GetAvailableBooks().Any(b => b.Id == bookId));
            Assert.IsTrue(service.GetBorrowedBooksByUser(userId).Any(b => b.Id == bookId));
        }

        [TestMethod]
        public void ReturnBook_ShouldReAddToAvailable()
        {
            var book = new Book { Title = "The Hobbit", Author = "Tolkien", ISBN = "456" };
            var user = new User { Name = "Bilbo", Email = "bilbo@shire.com" };

            service.AddBook(book);
            service.AddUser(user);

            int bookId = service.GetAllBooks().First().Id;
            int userId = service.GetUsers().First().Id;

            service.BorrowBook(userId, bookId);
            bool returned = service.ReturnBook(userId, bookId);

            Assert.IsTrue(returned);
            Assert.IsTrue(service.GetAvailableBooks().Any(b => b.Id == bookId));
            Assert.IsFalse(service.GetBorrowedBooksByUser(userId).Any(b => b.Id == bookId));
        }

        [TestMethod]
        public void UpdateBook_ShouldModifyBookDetails()
        {
            var book = new Book { Title = "Old Title", Author = "Old Author", ISBN = "111" };
            service.AddBook(book);

            var original = service.GetAllBooks().First();
            original.Title = "New Title";
            original.Author = "New Author";

            service.UpdateBook(original);

            var updated = service.GetAllBooks().First();
            Assert.AreEqual("New Title", updated.Title);
            Assert.AreEqual("New Author", updated.Author);
        }

        [TestMethod]
        public void AddUser_ShouldAddToUsersList()
        {
            var user = new User { Name = "Test User", Email = "test@example.com" };

            service.AddUser(user);
            var users = service.GetUsers();

            Assert.IsTrue(users.Any(u => u.Name == "Test User"));
        }


        [TestMethod]
        public void UpdateUser_ShouldChangeUserInfo()
        {
            var user = new User { Name = "Original", Email = "original@test.com" };
            service.AddUser(user);

            var addedUser = service.GetUsers().First();
            addedUser.Name = "Updated";
            addedUser.Email = "updated@test.com";

            service.UpdateUser(addedUser);

            var updated = service.GetUsers().First();
            Assert.AreEqual("Updated", updated.Name);
            Assert.AreEqual("updated@test.com", updated.Email);
        }

        [TestMethod]
        public void BorrowBook_ShouldFailIfUserOrBookNotFound()
        {
            var result1 = service.BorrowBook(999, 1); // Invalid user
            var result2 = service.BorrowBook(1, 999); // Invalid book

            Assert.IsFalse(result1);
            Assert.IsFalse(result2);
        }

        [TestMethod]
        public void ReturnBook_ShouldFailIfBookNotBorrowed()
        {
            var user = new User { Name = "Tyler", Email = "tyler@test.com" };
            service.AddUser(user);
            var userId = service.GetUsers().First().Id;

            var result = service.ReturnBook(userId, 999); // Book was never borrowed

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void DeleteUser_ShouldRemoveFromUserList()
        {
            var user = new User { Name = "Delete Me", Email = "bye@test.com" };
            service.AddUser(user);

            int userId = service.GetUsers().First().Id;
            service.DeleteUser(userId);

            Assert.IsFalse(service.GetUsers().Any(u => u.Id == userId));
        }
    }
}
