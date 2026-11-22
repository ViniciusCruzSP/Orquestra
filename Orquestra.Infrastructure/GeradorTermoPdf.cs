using System.Globalization;
using Orquestra.Application.Abstractions;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Orquestra.Infrastructure;

internal sealed class GeradorTermoPdf : IGeradorTermo
{
    static GeradorTermoPdf() => QuestPDF.Settings.License = LicenseType.Community;

    public byte[] Gerar(DadosTermo dados) => Document.Create(documento =>
    {
        documento.Page(pagina =>
        {
            pagina.Size(PageSizes.A4);
            pagina.Margin(46);
            pagina.DefaultTextStyle(estilo => estilo.FontSize(10));
            pagina.Header().PaddingBottom(18).Text("ORQUESTRA  |  Gestão de ativos de TI")
                .FontColor("#184278").SemiBold();
            pagina.Content().Column(coluna =>
            {
                coluna.Spacing(12);
                coluna.Item().Text("Termo de compromisso pela guarda de equipamentos")
                    .FontSize(20).Bold().FontColor("#184278");
                coluna.Item().Text("Declaro que recebi os equipamentos relacionados abaixo e assumo o compromisso de cuidar deles, informar ocorrências à organização e devolvê-los quando solicitado.");
                coluna.Item().Text($"Organização: {dados.Organizacao}");
                coluna.Item().Text($"Colaborador: {dados.Colaborador} · {dados.Email}");
                if (dados.Departamento is not null)
                    coluna.Item().Text($"Departamento: {dados.Departamento}");
                if (dados.CpfMascarado is not null)
                    coluna.Item().Text($"CPF: {dados.CpfMascarado}");
                if (dados.Cargo is not null)
                    coluna.Item().Text($"Cargo: {dados.Cargo}");
                coluna.Item().Text($"Entrega: {dados.EntregueEm.ToString("dd/MM/yyyy HH:mm 'UTC'zzz", CultureInfo.InvariantCulture)}");
                coluna.Item().Table(tabela =>
                {
                    tabela.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(3); });
                    tabela.Header(c =>
                    {
                        c.Cell().Background("#EAF2FB").Padding(6).Text("Patrimônio").SemiBold();
                        c.Cell().Background("#EAF2FB").Padding(6).Text("Equipamento").SemiBold();
                    });
                    foreach (var item in dados.Equipamentos)
                    {
                        tabela.Cell().BorderBottom(1).BorderColor("#D9D9D9").Padding(6).Text(item.Patrimonio);
                        var serie = string.IsNullOrWhiteSpace(item.Serie) ? "" : $" · Série {item.Serie}";
                        var tipo = item.Tipo is null ? "" : $" · {item.Tipo}";
                        tabela.Cell().BorderBottom(1).BorderColor("#D9D9D9").Padding(6)
                            .Text($"{item.Nome} · {item.Marca} {item.Modelo}{tipo}{serie}");
                    }
                });
                coluna.Item().ShowEntire().PaddingTop(34).AlignCenter().Column(assinatura =>
                {
                    assinatura.Item().Width(260).LineHorizontal(1);
                    assinatura.Item().PaddingTop(6).AlignCenter().Text("Assinatura do colaborador");
                    assinatura.Item().PaddingTop(8).AlignCenter().Text("Data: ____ / ____ / ______");
                });
            });
            pagina.Footer().Text(texto =>
            {
                texto.Span($"Custódia {dados.CustodiaId}  ·  Página ");
                texto.CurrentPageNumber();
                texto.Span(" de ");
                texto.TotalPages();
            });
        });
    }).GeneratePdf();
}
