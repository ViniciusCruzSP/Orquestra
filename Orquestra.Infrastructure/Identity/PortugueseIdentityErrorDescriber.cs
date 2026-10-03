using Microsoft.AspNetCore.Identity;

namespace Orquestra.Infrastructure.Identity;

internal sealed class PortugueseIdentityErrorDescriber : IdentityErrorDescriber
{
    private static IdentityError Error(string code, string description) => new()
    {
        Code = code,
        Description = description
    };

    public override IdentityError DefaultError() =>
        Error(nameof(DefaultError), "Ocorreu um erro inesperado.");

    public override IdentityError ConcurrencyFailure() =>
        Error(nameof(ConcurrencyFailure), "O registro foi alterado por outra operação. Tente novamente.");

    public override IdentityError PasswordMismatch() =>
        Error(nameof(PasswordMismatch), "A senha informada está incorreta.");

    public override IdentityError InvalidToken() =>
        Error(nameof(InvalidToken), "O token informado é inválido.");

    public override IdentityError LoginAlreadyAssociated() =>
        Error(nameof(LoginAlreadyAssociated), "Este acesso já está associado a outro usuário.");

    public override IdentityError InvalidUserName(string? userName) =>
        Error(nameof(InvalidUserName), $"O nome de usuário '{userName}' é inválido.");

    public override IdentityError InvalidEmail(string? email) =>
        Error(nameof(InvalidEmail), $"O e-mail '{email}' é inválido.");

    public override IdentityError DuplicateUserName(string userName) =>
        Error(nameof(DuplicateUserName), $"O nome de usuário '{userName}' já está em uso.");

    public override IdentityError DuplicateEmail(string email) =>
        Error(nameof(DuplicateEmail), $"O e-mail '{email}' já está em uso.");

    public override IdentityError InvalidRoleName(string? role) =>
        Error(nameof(InvalidRoleName), $"O perfil '{role}' é inválido.");

    public override IdentityError DuplicateRoleName(string role) =>
        Error(nameof(DuplicateRoleName), $"O perfil '{role}' já existe.");

    public override IdentityError UserAlreadyHasPassword() =>
        Error(nameof(UserAlreadyHasPassword), "O usuário já possui uma senha cadastrada.");

    public override IdentityError UserLockoutNotEnabled() =>
        Error(nameof(UserLockoutNotEnabled), "O bloqueio não está habilitado para este usuário.");

    public override IdentityError UserAlreadyInRole(string role) =>
        Error(nameof(UserAlreadyInRole), $"O usuário já possui o perfil '{role}'.");

    public override IdentityError UserNotInRole(string role) =>
        Error(nameof(UserNotInRole), $"O usuário não possui o perfil '{role}'.");

    public override IdentityError PasswordTooShort(int length) =>
        Error(nameof(PasswordTooShort), $"A senha deve ter pelo menos {length} caracteres.");

    public override IdentityError PasswordRequiresUniqueChars(int uniqueChars) =>
        Error(nameof(PasswordRequiresUniqueChars), $"A senha deve ter pelo menos {uniqueChars} caracteres diferentes.");

    public override IdentityError PasswordRequiresNonAlphanumeric() =>
        Error(nameof(PasswordRequiresNonAlphanumeric), "A senha deve ter pelo menos um símbolo.");

    public override IdentityError PasswordRequiresDigit() =>
        Error(nameof(PasswordRequiresDigit), "A senha deve ter pelo menos um número.");

    public override IdentityError PasswordRequiresLower() =>
        Error(nameof(PasswordRequiresLower), "A senha deve ter pelo menos uma letra minúscula.");

    public override IdentityError PasswordRequiresUpper() =>
        Error(nameof(PasswordRequiresUpper), "A senha deve ter pelo menos uma letra maiúscula.");

    public override IdentityError RecoveryCodeRedemptionFailed() =>
        Error(nameof(RecoveryCodeRedemptionFailed), "Não foi possível usar o código de recuperação.");
}
