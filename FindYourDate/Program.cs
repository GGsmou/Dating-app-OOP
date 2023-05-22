using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;

namespace FindYourDate
{ 
    class User
    {
        public string Name { get; set; }
        public char Gender { get; set; }
        public int ID { get; set; }
        public List<string> Info { get; set; }
        public List<string> Requirements { get; set; }
        public string Invitations { get; set; }

        public User(string name, char gender, int id, List<string> info, List<string> requirements)
        {
            Name = name;
            Gender = gender;
            ID = id;
            Info = info;
            Requirements = requirements;
            Invitations = "";
        }

        public bool isInvited()
        {
            return Invitations != "";
        }
    }

    class MainClass
    {
        private static DBClass database;
        private static DBClass archive;

        static void Main(string[] args)
        {
            database = new DBClass("database.json");
            archive = new DBClass("archive.json");

            while (true)
            {
                PrintClass.DrawMenu("Вхід", new string[] { "Обрати інсуючий профіль", "Створити новий профіль", "Вийти" });

                int choice = PrintClass.GetUserChoice(3);
                Console.Clear();

                switch (choice)
                {
                    case 1:
                        Login();
                        break;
                    case 2:
                        CreateNewAccount();
                        break;
                    case 3:
                        Console.WriteLine("Дякую за використання додатка. Завершення роботи...");
                        return;
                }

                Console.Clear();
            }
        }

        static void Login()
        {
            if (database.IsEmpty())
            {
                Console.WriteLine("Профілів нема, створіть новий.");
                return;
            }

            PrintClass.DrawTitle("Логін");

            Console.WriteLine("Оберіть ваш профіль:");
            List<User> users = database.GetAll();

            for (int i = 0; i < users.Count; i++)
            {
                User user = users[i];
                Console.WriteLine($"{i + 1}. {user.Name} - {user.ID}");
            }

            int choice = PrintClass.GetUserChoice(users.Count);
            User selectedUser = users[choice - 1];

            Console.WriteLine($"Ви увійшли як {selectedUser.Name}");
            MainMenu(selectedUser);
        }

        static void CreateNewAccount()
        {
            PrintClass.DrawTitle("Створення профілю");

            Console.Write("Уведіть ваше ім'я: ");
            string name = Console.ReadLine();

            Console.Write("Уведіть вашу стать (ч/ж): ");
            char gender = Console.ReadKey().KeyChar;
            Console.WriteLine();

            List<User> users = database.GetAll();
            int id = users.Count + 1000;

            List<string> info = new List<string>();
            Console.WriteLine("Введіть ваші характеристики (через кому): ");
            string[] infoArray = Console.ReadLine().Split(',');
            info.AddRange(infoArray);

            List<string> requirements = new List<string>();
            Console.WriteLine("Введіть характеристики потенційного партнера (через кому): ");
            string[] requirementsArray = Console.ReadLine().Split(',');
            requirements.AddRange(requirementsArray);

            User newUser = new User(name, gender, id, info, requirements);
            database.Add(newUser);

            Console.WriteLine("Аккаунт створено");
            MainMenu(newUser);
        }

        static void MainMenu(User user)
        {
            while (true)
            {
                PrintClass.DrawTitle("Головне меню");
                PrintClass.DrawMenu($"{user.Name} - {user.Info}", new string[] { "Додати профіль", "Видалити профіль", "Пошук партнера", "Пропозиції зустрічатися", "Вихід" });

                int choice = PrintClass.GetUserChoice(5);
                Console.Clear();

                switch (choice)
                {
                    case 1:
                        CreateNewAccount();
                        break;
                    case 2:
                        DeleteAccount();
                        break;
                    case 3:
                        SearchForPartners(user);
                        break;
                    case 4:
                        ViewInvitations(user);
                        break;
                    case 5:
                        Console.WriteLine("Дякую за використання додатка. Завершення роботи...");
                        return;
                }

                Console.Clear();
            }
        }

        static void DeleteAccount()
        {
            PrintClass.DrawTitle("Видалення профілю");
            Console.WriteLine("Оберіть ваш профіль:");

            List<User> users = database.GetAll();

            for (int i = 0; i < users.Count; i++)
            {
                User user = users[i];
                Console.WriteLine($"{i + 1}. {user.Name} - {user.ID}");
            }

            int choice = PrintClass.GetUserChoice(users.Count);
            User selectedUser = users[choice - 1];

            database.Delete(selectedUser.ID);
            Console.WriteLine($"Профіль '{selectedUser.Name}' видалено.");
        }

        static void SearchForPartners(User user)
        {
            PrintClass.DrawTitle("Пошук партнера");

            List<User> potentialPartners = GetSortedPartners(user);//

            if (potentialPartners.Count == 0)
            {
                Console.WriteLine("No potential partners found.");
                return;
            }

            Console.WriteLine("Potential Partners:");

            List<User> sortedPartners = SorterClass.SortUsersByName(potentialPartners);

            for (int i = 0; i < sortedPartners.Count; i++)
            {
                User partner = sortedPartners[i];
                Console.WriteLine($"{i + 1}. {partner.Name} - {partner.ID} - {string.Join(", ", partner.Info)}");
            }

            Console.Write("Choose a partner by number to send an invitation (0 to cancel): ");
            int choice = PrintClass.GetUserChoice(sortedPartners.Count);

            if (choice == 0)
                return;

            User selectedPartner = sortedPartners[choice - 1];
            user.Invitations.Add(selectedPartner.ID);

            Console.WriteLine($"Invitation sent to {selectedPartner.Name}!");
            database.Update<User>(user);
        }

        static List<User> GetSortedPartners(User user)
        {
            List<User> result = new List<User>();
            List<User> all = database.GetAll();

            foreach (User partner in all)
            {
                if (partner.ID != user.ID && !user.isInvited())
                {
                    foreach (string requirement in user.Requirements)
                    {
                        if (partner.Info.Contains(requirement))
                        {
                            result.Add(partner);
                        }
                    }
                }
            }

            return result;
        }

        static void ViewInvitations(User user)
        {
            PrintClass.DrawTitle("View Invitations");

            List<User> pendingInvitations = database.GetAll<User>(u => user.Invitations.Contains(u.ID));

            if (pendingInvitations.Count == 0)
            {
                Console.WriteLine("You have no pending invitations.");
                return;
            }

            Console.WriteLine("Pending Invitations:");

            List<User> sortedInvitations = SorterClass.SortUsersByName(pendingInvitations);

            for (int i = 0; i < sortedInvitations.Count; i++)
            {
                User invitation = sortedInvitations[i];
                Console.WriteLine($"{i + 1}. {invitation.Name} - {invitation.ID} - {string.Join(", ", invitation.Info)}");
            }

            Console.Write("Choose an invitation by number to accept (0 to cancel): ");
            int choice = PrintClass.GetUserChoice(sortedInvitations.Count);

            if (choice == 0)
                return;

            User acceptedInvitation = sortedInvitations[choice - 1];

            if (user.ID < acceptedInvitation.ID)
            {
                archive.Add<User>(user);
                archive.Add<User>(acceptedInvitation);
            }
            else
            {
                archive.Add<User>(acceptedInvitation);
                archive.Add<User>(user);
            }

            database.Delete<User>(u => u.ID == user.ID);
            database.Delete<User>(u => u.ID == acceptedInvitation.ID);

            Console.WriteLine($"You and {acceptedInvitation.Name} are now friends!");
        }
    }

    class PrintClass
    {
        public static void DrawTitle(string title)
        {
            Console.WriteLine($"---{title}---");
        }

        public static void DrawMenu(string title, string[] options)
        {
            DrawTitle(title);

            for (int i = 0; i < options.Length; i++)
            {
                Console.WriteLine($"{i + 1}. {options[i]}");
            }
        }

        public static int GetUserChoice(int maxChoice)
        {
            int choice;
            Console.Write("Команда: ");

            while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > maxChoice)
            {
                Console.WriteLine("Не коректна команда");
                Console.Write("Команда: ");
            }
            return choice;
        }
    }

    class DBClass
    {
        private string filePath;

        public DBClass(string filePath)
        {
            this.filePath = filePath;
        }

        public List<User> GetAll()
        {
            if (!File.Exists(filePath))
                return new List<User>();

            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<List<User>>(json);
        }

        public void Add<T>(T item)
        {
            List<T> items = GetAll<T>();
            items.Add(item);
            SaveAll(items);
        }

        public void Delete(int id)
        {
            List<User> items = GetAll<User>();
            items.RemoveAll(u => u.ID == id);
            SaveAll(items);
        }

        public void Update<T>(T item)
        {
            List<T> items = GetAll<T>();
            int index = items.FindIndex(x => x.Equals(item));
            if (index != -1)
            {
                items[index] = item;
                SaveAll(items);
            }
        }

        public bool IsEmpty()
        {
            return !File.Exists(filePath) || new FileInfo(filePath).Length == 0;
        }

        private void SaveAll<T>(List<T> items)
        {
            string json = JsonConvert.SerializeObject(items, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }

    class SorterClass
    {
        public static List<User> SortUsersByName(List<User> users)
        {
            return users.OrderBy(u => u.Name).ToList();
        }
    }
}
