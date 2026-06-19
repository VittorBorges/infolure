using System.Text.RegularExpressions;
using FluentValidation;

namespace Infolure.Api.Features.Admin;

// Feature 005 — validação estrutural do payload de escrita de iscas (FR-009/FR-011).
// A unicidade do slug é verificada no LureWriteService (→ 409), não aqui.
public partial class LureWriteValidator : AbstractValidator<LureWriteRequest>
{
    [GeneratedRegex("^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")]
    private static partial Regex HexRegex();

    private static readonly string[] WaterTypes = ["freshwater", "saltwater", "both"];
    private static readonly string[] Statuses = ["draft", "published", "archived"];

    public LureWriteValidator()
    {
        RuleFor(x => x.Slug).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.LureType).NotEmpty();
        RuleFor(x => x.WaterType).Must(w => w is null || WaterTypes.Contains(w))
            .WithMessage("water_type inválido");
        RuleFor(x => x.Status).Must(s => s is null || Statuses.Contains(s))
            .WithMessage("status inválido");

        // FR-007 — ≥1 configuração; cada uma com label (peso e anzol são opcionais).
        RuleFor(x => x.Configurations).NotEmpty().WithMessage("é necessária pelo menos uma configuração");
        RuleForEach(x => x.Configurations).ChildRules(c =>
        {
            c.RuleFor(z => z.Label).NotEmpty().WithMessage("rótulo da configuração obrigatório");
            // Peso opcional; quando indicado, deve ser > 0.
            c.RuleFor(z => z.WeightG).GreaterThan(0).When(z => z.WeightG.HasValue)
                .WithMessage("peso da configuração deve ser > 0");
        });

        // FR-005..FR-009 — cores opcionais; cada cor não-vazia (nome OU ≥1 hex); hex válidos.
        // Duplicados de hex na mesma cor são PERMITIDOS (podem ter textura diferente).
        RuleForEach(x => x.Colors).ChildRules(c =>
        {
            c.RuleFor(col => col)
                .Must(col => !string.IsNullOrWhiteSpace(col.NamePt) || (col.HexCodes?.Count ?? 0) > 0)
                .WithMessage("cor vazia: indique um nome ou pelo menos um código hex");
            c.RuleForEach(col => col.HexCodes).ChildRules(h =>
                h.RuleFor(z => z.Hex).Must(hex => hex is not null && HexRegex().IsMatch(hex))
                    .WithMessage("código hex inválido (use #RGB ou #RRGGBB)"));
        });
    }
}
