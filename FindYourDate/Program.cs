using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Xml;
using Newtonsoft.Json;

namespace FindYourDate
{ 
    class User
    {
        public string Name { get; set; }
        public string Gender { get; set; }
        public int ID { get; set; }
        public List<string> Info { get; set; }
        public List<string> Requirements { get; set; }
        public int Invitations { get; set; }
        public bool HasBeenInvited { get; set; }

        public User(string name, string gender, int id, List<string> info, List<string> requirements)
        {
            Name = name;
            Gender = gender;
            ID = id;
            Info = info;
            Requirements = requirements;
            Invitations = -1;
            HasBeenInvited = false;
        }

        public bool isInvited()
        {
            return Invitations != -1;
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
                PrintClass.DrawMenu("Вхiд", new string[] { "Обрати iнсуючий профiль", "Створити новий профiль", "Вийти" });

                int choice = PrintClass.GetUserChoice(3);
                Console.Clear();

                switch (choice)
                {
                    case 1:
                        Login();
                        break;
                    case 2:
                        CreateNewAccount(true);
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
                Console.WriteLine("Профiлiв нема, створiть новий.");
                Thread.Sleep(2000);
                return;
            }

            PrintClass.DrawTitle("Логiн");

            Console.WriteLine("Оберiть ваш профiль:");
            List<User> users = database.GetAll();

            for (int i = 0; i < users.Count; i++)
            {
                User user = users[i];
                Console.WriteLine($"{i + 1}. {user.Name} - {user.ID}");
            }

            int choice = PrintClass.GetUserChoice(users.Count);
            User selectedUser = users[choice - 1];

            Console.WriteLine($"Ви увiйшли як {selectedUser.Name}");
            MainMenu(selectedUser);
        }

        static void CreateNewAccount(bool isStart)
        {
            static string NameProccesor()
            {
                Console.Write("Уведiть ваше iм'я: ");
                string inp = Console.ReadLine();

                if (inp.Length > 100)
                {
                    Console.WriteLine("Задовге iм'я");
                    return NameProccesor();
                }

                return inp;
            }
            static string GenderProccesor()
            {
                Console.Write("Укажiть вашу стать (ч/ж): ");
                string inp = Console.ReadLine();

                if (inp != "ч" && inp != "ж")
                {
                    Console.WriteLine("Не коректне значення");
                    return GenderProccesor();
                }

                return inp;
            }

            PrintClass.DrawTitle("Створення профiлю");

            string name = NameProccesor();
            string gender = GenderProccesor();

            List<User> users = database.GetAll();
            int id = Int32.Parse(File.ReadAllText("id.txt"));
            File.WriteAllText("id.txt", $"{id + 1}");

            List<string> info = new List<string>();
            Console.WriteLine("Введiть вашi характеристики (через кому): ");
            string[] infoArray = Console.ReadLine().Split(',');
            info.AddRange(infoArray);

            List<string> requirements = new List<string>();
            Console.WriteLine("Введiть характеристики потенцiйного партнера (через кому): ");
            string[] requirementsArray = Console.ReadLine().Split(',');
            requirements.AddRange(requirementsArray);

            User newUser = new User(name, gender, id, info, requirements);
            database.Add(newUser);

            Console.WriteLine("\nАккаунт створено");
            
            if (isStart)
            {
                MainMenu(newUser);
            }
        }

        static void MainMenu(User user)
        {
            while (true)
            {
                PrintClass.DrawTitle("Головне меню");

                string isAccecible = "";
                if (user.isInvited())
                {
                    isAccecible = "(не доступно)";
                }

                PrintClass.DrawMenu($"{user.Name} - {user.ID}", new string[] { "Додати профiль", "Видалити профiль", $"Пошук партнера{isAccecible}", "Пропозицiї зустрiчатися", "Вихiд" });

                int choice = PrintClass.GetUserChoice(5);
                Console.Clear();

                switch (choice)
                {
                    case 1:
                        CreateNewAccount(false);
                        break;
                    case 2:
                        DeleteAccount(user.ID);
                        break;
                    case 3:
                        if (!user.isInvited())
                        { 
                            SearchForPartners(user);
                        }
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

        static void DeleteAccount(int currentUserID)
        {
            PrintClass.DrawTitle("Видалення профiлю");
            Console.WriteLine("Оберiть профiль цифорою (0 - вихiд):");

            List<User> users = database.GetAll();

            for (int i = 0; i < users.Count; i++)
            {
                User user = users[i];
                Console.WriteLine($"{i + 1}. {user.Name} - {user.ID}");
            }

            int choice = PrintClass.GetUserChoice(users.Count);

            if (choice == 0)
            {
                return;
            }

            User selectedUser = users[choice - 1];

            if (selectedUser.ID == currentUserID)
            {
                Console.WriteLine("\nНе можна видалити активний профiль");
                Thread.Sleep(2000);
                return;
            }

            if (selectedUser.isInvited())
            {
                User invited = GetPartnedByID(selectedUser.Invitations);
                invited.Invitations = -1;
                invited.HasBeenInvited = false;
                database.Update(invited);
            }
            database.Delete(selectedUser.ID);
            Console.WriteLine($"Профiль '{selectedUser.Name}' видалено.");
        }

        static void SearchForPartners(User user)
        {
            PrintClass.DrawTitle("Пошук партнера");

            List<User> potentialPartners = GetFilteredPartners(user);

            if (potentialPartners.Count == 0)
            {
                Console.WriteLine("Для вас ще не знайдено пари.");
                Thread.Sleep(2000);
                return;
            }

            Console.WriteLine("Потенцiйнi партнери:");

            List<User> sortedPartners = SorterClass.SortUsersByName(potentialPartners);
            Console.Write("Оберiть номер користувача для створення запрошення (0 - вихiд):\n\n");

            for (int i = 0; i < sortedPartners.Count; i++)
            {
                User partner = sortedPartners[i];
                Console.WriteLine($"{i + 1}. {partner.Name} - {partner.Gender} - {partner.ID} - {string.Join(", ", partner.Info)}");
            }

            int choice = PrintClass.GetUserChoice(sortedPartners.Count);

            if (choice == 0)
                return;

            User selectedPartner = sortedPartners[choice - 1];
            user.Invitations = selectedPartner.ID;
            selectedPartner.Invitations = user.ID;
            selectedPartner.HasBeenInvited = true;

            Console.WriteLine($"\nЗапрошення вiдправлено {selectedPartner.Name}!");
            database.Update(user);
            database.Update(selectedPartner);
            Thread.Sleep(2000);
        }

        static List<User> GetFilteredPartners(User user)
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
                            break;
                        }
                    }
                }
            }

            return result;
        }

        static User GetPartnedByID(int ID)
        {
            List<User> all = database.GetAll();

            foreach (User partner in all)
            {
                if (partner.ID == ID)
                {
                    return partner;
                }
            }

            return null;
        }

        static void ViewInvitations(User user)
        {
            static void CancellInvitation(User user, User userInvitation)
            {
                user.Invitations = -1;
                userInvitation.Invitations = -1;
                userInvitation.HasBeenInvited = false;
                user.HasBeenInvited = false;

                database.Update(userInvitation);
                database.Update(user);
            }

            static void AcceptInvitation(User user, User userInvitation)
            {
                archive.Add(user);
                archive.Add(userInvitation);

                database.Delete(user.ID);
                database.Delete(userInvitation.ID);
            }

            PrintClass.DrawTitle("Пропозицiї зустрiчатися");

            User userInvitation = GetPartnedByID(user.Invitations);

            if (userInvitation == null)
            {
                Console.WriteLine("Пропозицiї вiдсутнi");
                Thread.Sleep(2000);
                return;
            }

            Console.WriteLine($"{userInvitation.Name} - {userInvitation.ID} - {string.Join(", ", userInvitation.Info)}");

            if (!user.HasBeenInvited)
            {
                PrintClass.DrawMenu("(створено вами)", new string[] { "Вiдхилити", "Вихiд" });
                int choice = PrintClass.GetUserChoice(2);

                switch (choice)
                {
                    case 1:
                        CancellInvitation(user, userInvitation);
                        Console.WriteLine("\nПропозицiя вiдмiнена");
                        Thread.Sleep(2000);
                        return;
                    case 2:
                        return;
                }
            }
            if (user.HasBeenInvited)
            {
                PrintClass.DrawMenu("", new string[] { "Вiдхилити", "Приняти", "Вихiд" });
                int choice = PrintClass.GetUserChoice(3);

                switch (choice)
                {
                    case 1:
                        CancellInvitation(user, userInvitation);
                        Console.WriteLine("\nПропозицiя вiдмiнена.");
                        Thread.Sleep(2000);
                        return;
                    case 2:
                        AcceptInvitation(user, userInvitation);
                        Console.WriteLine("\nПару знайдено!");
                        Console.WriteLine("\n\nНатиснiть будь-яку для виходу...");
                        Console.ReadKey();
                        System.Environment.Exit(1);
                        return;
                    case 3:
                        return;
                }
            }
        }
    }

    class PrintClass
    {
        public static void DrawTitle(string title)
        {
            Console.WriteLine($"\n---{title}---\n");
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
            Console.Write("\n> ");

            while (!int.TryParse(Console.ReadLine(), out choice) || choice < 1 || choice > maxChoice)
            {
                if (choice == 0)
                {
                    return 0;
                }

                Console.WriteLine("Не коректна команда");
                Console.Write("\n> ");
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

        public void Add(User item)
        {
            List<User> items = GetAll();
            items.Add(item);
            SaveAll(items);
        }

        public void Delete(int id)
        {
            List<User> items = GetAll();

            items.RemoveAll(u => u.ID == id);
            SaveAll(items);
        }

        public void Update(User inp)
        {
            List<User> items = GetAll();
            int index = 0;
            
            foreach (User item in items)
            {
                if (item.ID == inp.ID)
                {
                    items[index] = inp;
                    SaveAll(items);
                    break;
                }

                index++;
            }
        }

        public bool IsEmpty()
        {
            return !File.Exists(filePath) || new FileInfo(filePath).Length == 0;
        }

        public void SaveAll(List<User> items)
        {
            string json = JsonConvert.SerializeObject(items);
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
