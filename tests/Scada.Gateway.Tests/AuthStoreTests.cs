using Scada.Gateway;
using Xunit;

namespace Scada.Gateway.Tests;

public sealed class AuthStoreTests
{
    [Fact]
    public async Task FirstAdminCanBeCreatedAndAuthenticatedWithoutStoringPlaintextPassword()
    {
        await using var database = TestDatabase.Create();
        var store = new AuthStore(database.Path);

        await store.InitializeAsync();
        var created = await store.CreateFirstAdminAsync("admin", "correct horse battery staple");
        var authenticated = await store.AuthenticateAsync("admin", "correct horse battery staple");

        Assert.Equal("admin", created.UserName);
        Assert.NotNull(authenticated);
        Assert.Equal(AuthRole.Admin, authenticated!.Role);
        Assert.DoesNotContain("correct horse battery staple", await database.ReadTextAsync());
    }

    [Fact]
    public async Task FirstAdminCreationIsOneTimeAndDuplicateUsernamesAreRejected()
    {
        await using var database = TestDatabase.Create();
        var store = new AuthStore(database.Path);
        await store.InitializeAsync();
        await store.CreateFirstAdminAsync("admin", "password-1");

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.CreateFirstAdminAsync("second", "password-2"));
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            store.CreateUserAsync("admin", "password-3", AuthRole.Viewer));
    }

    [Fact]
    public async Task DisabledUserCannotAuthenticateAndRolesCanBeChanged()
    {
        await using var database = TestDatabase.Create();
        var store = new AuthStore(database.Path);
        await store.InitializeAsync();
        var user = await store.CreateFirstAdminAsync("admin", "password");
        var operatorUser = await store.CreateUserAsync("operator", "password", AuthRole.Operator);

        Assert.Null(await store.AuthenticateAsync("operator", "wrong"));
        await store.SetEnabledAsync(operatorUser.Id, false);
        Assert.Null(await store.AuthenticateAsync("operator", "password"));

        await store.SetRoleAsync(user.Id, AuthRole.Engineer);
        var changed = await store.GetUserAsync(user.Id);
        Assert.Equal(AuthRole.Engineer, changed!.Role);
    }

    [Fact]
    public async Task MissingUserAndWrongPasswordReturnNull()
    {
        await using var database = TestDatabase.Create();
        var store = new AuthStore(database.Path);
        await store.InitializeAsync();

        Assert.Null(await store.AuthenticateAsync("missing", "password"));
    }

    private sealed class TestDatabase : IAsyncDisposable
    {
        private TestDatabase(string path) => Path = path;

        public string Path { get; }

        public static TestDatabase Create() =>
            new(System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"scada-auth-{Guid.NewGuid():N}.db"));

        public async Task<string> ReadTextAsync()
        {
            if (!File.Exists(Path))
            {
                return string.Empty;
            }

            return await File.ReadAllTextAsync(Path);
        }

        public ValueTask DisposeAsync()
        {
            File.Delete(Path);
            File.Delete(Path + "-wal");
            File.Delete(Path + "-shm");
            return ValueTask.CompletedTask;
        }
    }
}
