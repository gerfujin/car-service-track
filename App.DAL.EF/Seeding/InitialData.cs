namespace App.DAL.EF.Seeding;

public static class InitialData
{
    public static readonly string[] Roles = [
        "admin",
        "mechanic",
        "client"
    ];

    public static readonly (string email, string password, string[] roles)[] Users = [
        ("admin@carservice.ee", "Admin.12345", ["admin"]),
        ("mechanic@carservice.ee", "Mech.12345", ["mechanic"]),
        ("client@carservice.ee", "Client.12345", ["client"]),
    ];
}
