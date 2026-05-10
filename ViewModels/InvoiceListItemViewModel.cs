using System.Globalization;
using TestHub.Models.Invoice;

namespace TestHub.ViewModels;

/// <summary>
/// Per-row VM for an invoice card on the Invoices list. Owns all of the
/// presentation logic (invoice number formatting, status pill colours,
/// "Non-project" badge, hide/show of PDF and Resend buttons) so the data
/// template stays declarative.
/// </summary>
public sealed class InvoiceListItemViewModel : BaseViewModel
{
    private static readonly Color PaidBadgeBg     = Color.FromArgb("#DCFCE7");
    private static readonly Color PaidBadgeText   = Color.FromArgb("#16A34A");
    private static readonly Color SentBadgeBg     = Color.FromArgb("#FEF3C7");
    private static readonly Color SentBadgeText   = Color.FromArgb("#B45309");
    private static readonly Color NeutralBadgeBg  = Color.FromArgb("#E5E7EB");
    private static readonly Color NeutralBadgeText = Color.FromArgb("#475569");

    private readonly InvoiceListItem _model;

    public InvoiceListItemViewModel(InvoiceListItem model)
    {
        _model = model ?? throw new ArgumentNullException(nameof(model));
    }

    public int ContractMilestoneId => _model.ContractMilestoneId;
    public int ContractId => _model.ContractId;

    /// <summary>Formatted "INV-001" identifier shown at the top-left of the card.</summary>
    public string InvoiceNumber =>
        string.Format(CultureInfo.InvariantCulture, "INV-{0:D3}", _model.ContractMilestoneId);

    /// <summary>
    /// "Project name" line under the invoice number. Falls back to the
    /// milestone name for non-project (ad-hoc) invoices that don't carry
    /// a real project.
    /// </summary>
    public string ProjectLine =>
        !string.IsNullOrWhiteSpace(_model.ProjectName) ? _model.ProjectName!
            : !string.IsNullOrWhiteSpace(_model.MilestoneName) ? _model.MilestoneName!
            : "—";

    public string CustomerName =>
        string.IsNullOrWhiteSpace(_model.CustomerName) ? "—" : _model.CustomerName!;

    /// <summary>"$7,500" style amount on the invoice card.</summary>
    public string AmountDisplay =>
        string.Format(CultureInfo.InvariantCulture, "${0:N0}", _model.Amount);

    /// <summary>
    /// "Generated On" date label. For paid invoices we prefer the
    /// <c>paidAmountDate</c> when the server filled it; otherwise we fall
    /// back to <c>invoiceGeneratedAtUtc</c>.
    /// </summary>
    public string GeneratedDateDisplay
    {
        get
        {
            var date = _model.InvoiceGeneratedAtUtc ?? _model.PaidAmountDate;
            return date is null
                ? "—"
                : date.Value.ToLocalTime().ToString("d/M/yyyy", CultureInfo.InvariantCulture);
        }
    }

    public bool IsPaid => _model.IsPaid;

    /// <summary>
    /// True for ad-hoc/standalone invoices that aren't tied to a project
    /// contract — drives the small "Non-project" pill next to the invoice
    /// number on the card.
    /// </summary>
    public bool IsNonProject => _model.ContractId == 0;

    /// <summary>Visibility of the "PDF" download pill.</summary>
    public bool ShowPdfButton => _model.IsInvoiceGenerated;

    /// <summary>Visibility of the "Resend" pill (only meaningful when unpaid).</summary>
    public bool ShowResendButton => !_model.IsPaid && _model.IsInvoiceGenerated;

    /// <summary>Right-hand status pill text — "Paid", "Sent", "Pending"…</summary>
    public string StatusBadgeLabel
    {
        get
        {
            if (_model.IsPaid) return "Paid";
            if (!string.IsNullOrWhiteSpace(_model.Status)) return _model.Status!;
            return _model.IsInvoiceGenerated ? "Sent" : "Pending";
        }
    }

    public Color StatusBadgeBackground => _model.IsPaid
        ? PaidBadgeBg
        : IsSentLike(_model.Status, _model.IsInvoiceGenerated) ? SentBadgeBg : NeutralBadgeBg;

    public Color StatusBadgeText => _model.IsPaid
        ? PaidBadgeText
        : IsSentLike(_model.Status, _model.IsInvoiceGenerated) ? SentBadgeText : NeutralBadgeText;

    private static bool IsSentLike(string? status, bool isInvoiceGenerated)
    {
        if (!string.IsNullOrWhiteSpace(status))
        {
            var normalised = status.Trim().ToLowerInvariant();
            return normalised is "sent" or "pending" or "outstanding" or "due";
        }
        return isInvoiceGenerated;
    }
}
