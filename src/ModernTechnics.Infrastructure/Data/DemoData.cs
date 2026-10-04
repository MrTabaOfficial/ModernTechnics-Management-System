using ModernTechnics.Core.Domain;
using ModernTechnics.Core.Security;

namespace ModernTechnics.Infrastructure.Data;

/// <summary>Sample content so the application is explorable straight after the first launch.</summary>
public static class DemoData
{
    public const string Password = "Demo1234";

    public const string AdministratorEmail = "admin@moderntechnics.ge";
    public const string ManagerEmail = "manager@moderntechnics.ge";
    public const string WarehouseEmail = "warehouse@moderntechnics.ge";
    public const string SalesEmail = "sales@moderntechnics.ge";

    public static void Seed(AppDbContext db, IPasswordHasher hasher, DateTime utcNow)
    {
        var passwordHash = hasher.Hash(Password);
        db.Users.AddRange(
            new UserAccount { Email = AdministratorEmail, PasswordHash = passwordHash, Role = Role.Administrator },
            new UserAccount { Email = ManagerEmail, PasswordHash = passwordHash, Role = Role.Manager },
            new UserAccount { Email = WarehouseEmail, PasswordHash = passwordHash, Role = Role.WarehouseOperator },
            new UserAccount { Email = SalesEmail, PasswordHash = passwordHash, Role = Role.SalesAssociate });

        var director = new Position { Name = "Director" };
        var manager = new Position { Name = "Store Manager" };
        var accountant = new Position { Name = "Accountant" };
        var hr = new Position { Name = "HR Specialist" };
        var warehouse = new Position { Name = "Warehouse Operator" };
        var sales = new Position { Name = "Sales Consultant" };
        var technician = new Position { Name = "Service Technician" };
        db.Positions.AddRange(director, manager, accountant, hr, warehouse, sales, technician);

        db.Employees.AddRange(
            Employee("01024058311", "Giorgi", "Beridze", 1984, 3, 14, "12 Rustaveli Ave, Tbilisi", "+995 599 10 20 30", MaritalStatus.Married, director, 6500m),
            Employee("01017042265", "Nino", "Kapanadze", 1990, 7, 2, "4 Chavchavadze Ave, Tbilisi", "+995 577 21 43 65", MaritalStatus.Married, manager, 3800m),
            Employee("35001093847", "Luka", "Gelashvili", 1993, 11, 21, "27 Pekini St, Tbilisi", "+995 555 34 56 78", MaritalStatus.Single, accountant, 2900m),
            Employee("01030017592", "Mariam", "Tsiklauri", 1995, 1, 9, "8 Kostava St, Tbilisi", "+995 598 45 67 89", MaritalStatus.Single, hr, 2400m),
            Employee("61004028813", "Davit", "Lomidze", 1988, 5, 30, "15 Gorgiladze St, Batumi", "+995 591 56 78 90", MaritalStatus.Married, warehouse, 1900m),
            Employee("01011066724", "Ana", "Mchedlishvili", 1998, 9, 17, "3 Vazha-Pshavela Ave, Tbilisi", "+995 568 67 89 01", MaritalStatus.Single, sales, 1700m),
            Employee("60001055139", "Saba", "Kvaratskhelia", 1997, 2, 12, "19 Tsereteli Ave, Kutaisi", "+995 579 78 90 12", MaritalStatus.Single, sales, 1700m),
            Employee("01008039456", "Tamar", "Jorbenadze", 1991, 12, 5, "41 Agmashenebeli Ave, Tbilisi", "+995 593 89 01 23", MaritalStatus.Divorced, sales, 1850m),
            Employee("01019074681", "Irakli", "Shengelia", 1986, 8, 26, "6 Tamarashvili St, Tbilisi", "+995 551 90 12 34", MaritalStatus.Married, technician, 2200m),
            Employee("01026081207", "Elene", "Abashidze", 1999, 4, 3, "22 Nutsubidze St, Tbilisi", "+995 574 01 23 45", MaritalStatus.Single, warehouse, 1800m));

        var customers = new[]
        {
            Customer("01005012345", "Levan", "Maisuradze", 1987, 6, 11, "10 Barnovi St, Tbilisi", "+995 599 11 22 33"),
            Customer("01012023456", "Salome", "Gogoladze", 1994, 10, 23, "5 Paliashvili St, Tbilisi", "+995 577 22 33 44"),
            Customer("61001034567", "Nika", "Dolidze", 1992, 3, 8, "30 Chavchavadze St, Batumi", "+995 555 33 44 55"),
            Customer("01021045678", "Keti", "Zarandia", 1989, 12, 19, "17 Abashidze St, Tbilisi", "+995 598 44 55 66"),
            Customer("60002056789", "Zurab", "Chikovani", 1979, 7, 27, "2 Rustaveli Ave, Kutaisi", "+995 591 55 66 77"),
            Customer("01003067890", "Sopho", "Natroshvili", 2000, 1, 15, "9 Kazbegi Ave, Tbilisi", "+995 568 66 77 88"),
        };
        db.Customers.AddRange(customers);

        var products = new[]
        {
            Product("Galaxy S24 Ultra 256GB", "Samsung", 2024, 3499m, 18, 8),
            Product("iPhone 15 Pro 128GB", "Apple", 2023, 3799m, 12, 7),
            Product("MacBook Air 13\" M3", "Apple", 2024, 4299m, 7, 6),
            Product("ThinkPad X1 Carbon Gen 12", "Lenovo", 2024, 5899m, 5, 2),
            Product("PlayStation 5 Slim", "Sony", 2023, 1899m, 14, 9),
            Product("WH-1000XM5 Headphones", "Sony", 2022, 999m, 22, 11),
            Product("OLED C3 55\" TV", "LG", 2023, 3999m, 6, 2),
            Product("Redmi Note 13 Pro", "Xiaomi", 2024, 949m, 30, 15),
            Product("ROG Strix G16 Laptop", "ASUS", 2024, 4999m, 4, 1),
            Product("AirPods Pro 2", "Apple", 2023, 799m, 25, 12),
            Product("Galaxy Tab S9 FE", "Samsung", 2023, 1399m, 9, 0),
            Product("Pixel 8a 128GB", "Google", 2024, 1699m, 8, 9),
        };
        db.Products.AddRange(products);

        // (days ago, product index, customer index or -1 for a walk-in, quantity)
        (int DaysAgo, int Product, int Customer, int Quantity)[] history =
        [
            (0, 5, 0, 1), (0, 7, -1, 2), (1, 0, 1, 1), (1, 9, -1, 1), (2, 4, 2, 1), (2, 7, 3, 1),
            (3, 1, 4, 1), (3, 5, -1, 2), (4, 9, 5, 2), (4, 11, 0, 1), (5, 2, 1, 1), (5, 7, -1, 3),
            (6, 6, 3, 1), (6, 4, -1, 1), (9, 3, 4, 1), (12, 0, 2, 1), (16, 8, 5, 1), (21, 9, -1, 2),
        ];
        // Inserted oldest first, so order numbers rise with time.
        db.Orders.AddRange(history
            .Select(sale => new Order
            {
                Product = products[sale.Product],
                Customer = sale.Customer < 0 ? null : customers[sale.Customer],
                Quantity = sale.Quantity,
                UnitPrice = products[sale.Product].Price,
                PlacedAtUtc = utcNow.AddDays(-sale.DaysAgo).AddMinutes(-37 * (sale.Product + 1)),
            })
            .OrderBy(order => order.PlacedAtUtc));

        db.JobApplications.AddRange(
            Application("Giga", "Tabatadze", 2001, 5, 6, "+995 599 70 80 90", sales, ApplicationStatus.New, utcNow.AddDays(-1),
                "I have followed consumer electronics for years and worked part-time in retail while studying. I would like to grow into a full-time sales role."),
            Application("Lika", "Kobakhidze", 1996, 9, 28, "+995 577 80 90 10", accountant, ApplicationStatus.Interview, utcNow.AddDays(-4),
                "Four years of bookkeeping experience in a trading company, confident with payroll and monthly reporting."),
            Application("Tornike", "Mamulashvili", 1993, 2, 17, "+995 555 90 10 20", technician, ApplicationStatus.New, utcNow.AddDays(-6),
                "Certified in smartphone and laptop repair; previously ran a small service desk."),
            Application("Natia", "Surmanidze", 1990, 11, 1, "+995 598 10 20 30", manager, ApplicationStatus.Rejected, utcNow.AddDays(-15),
                "Seven years in retail management, most recently leading a team of twelve."));
    }

    private static Employee Employee(
        string personalId, string firstName, string lastName, int year, int month, int day,
        string address, string phone, MaritalStatus maritalStatus, Position position, decimal salary) => new()
        {
            PersonalId = personalId,
            FirstName = firstName,
            LastName = lastName,
            BirthDate = new DateOnly(year, month, day),
            Address = address,
            Phone = phone,
            Email = $"{firstName}.{lastName}@moderntechnics.ge".ToLowerInvariant(),
            MaritalStatus = maritalStatus,
            Position = position,
            Salary = salary,
        };

    private static Customer Customer(
        string personalId, string firstName, string lastName, int year, int month, int day,
        string address, string phone) => new()
        {
            PersonalId = personalId,
            FirstName = firstName,
            LastName = lastName,
            BirthDate = new DateOnly(year, month, day),
            Address = address,
            Phone = phone,
        };

    private static Product Product(
        string name, string manufacturer, int releaseYear, decimal price, int warehouseStock, int storeStock) => new()
        {
            Name = name,
            Manufacturer = manufacturer,
            ReleaseYear = releaseYear,
            Price = price,
            WarehouseStock = warehouseStock,
            StoreStock = storeStock,
        };

    private static JobApplication Application(
        string firstName, string lastName, int year, int month, int day, string phone,
        Position position, ApplicationStatus status, DateTime submittedAtUtc, string letter) => new()
        {
            FirstName = firstName,
            LastName = lastName,
            BirthDate = new DateOnly(year, month, day),
            Phone = phone,
            Position = position,
            Status = status,
            SubmittedAtUtc = submittedAtUtc,
            MotivationLetter = letter,
        };
}
