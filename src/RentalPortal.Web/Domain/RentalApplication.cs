using RentalPortal.Web.Enums;

namespace RentalPortal.Web.Domain;

public class RentalApplication
{
    public int Id { get; private set; }

    public int UnitId { get; private set; }

    public Unit Unit { get; private set; } = null!;

    public string ApplicantId { get; private set; } = null!;

    public ApplicationUser Applicant { get; private set; } = null!;

    public ApplicationStatus Status { get; private set; } = ApplicationStatus.Draft;

    public string? FirstName { get; private set; }

    public string? LastName { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? CurrentAddress { get; private set; }

    public bool ApplicantInfoCompleted { get; private set; }

    public bool ResidenceHistoryCompleted { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public List<Residence> Residences { get; } = new List<Residence>();

    public List<ApplicationStatusHistory> StatusHistory { get; } = new List<ApplicationStatusHistory>();

    public Lease? Lease { get; private set; }

    public bool IsEditable =>
        Status is ApplicationStatus.Draft
        or ApplicationStatus.Returned;

    public bool IsTerminal =>
        Status is ApplicationStatus.Approved
        or ApplicationStatus.Denied
        or ApplicationStatus.Withdrawn;

    public bool CanSubmit => IsEditable && ApplicantInfoCompleted && ResidenceHistoryCompleted;

    public bool CanWithdraw => !IsTerminal;

    public bool CanReview => Status == ApplicationStatus.Submitted;

    public static RentalApplication NewApplication(
        int unitId,
        ApplicationUser applicant,
        DateTime createdUtc)
    {
        var application = new RentalApplication
        {
            UnitId = unitId,
            ApplicantId = applicant.Id,
            FirstName = applicant.FirstName,
            LastName = applicant.LastName,
            Email = applicant.Email,
            CreatedAtUtc = createdUtc
        };

        application.StatusHistory.Add(new ApplicationStatusHistory
        {
            ToStatus = ApplicationStatus.Draft,
            ChangedById = applicant.Id,
            ChangedAtUtc = createdUtc
        });

        return application;
    }

    public void UpdateApplicant(
        string firstName,
        string lastName,
        string phone,
        string email,
        string currentAddress)
    {
        CheckEditable();

        FirstName = firstName.Trim();

        LastName = lastName.Trim();

        Phone = phone.Trim();

        Email = email.Trim();

        CurrentAddress = currentAddress.Trim();

        ApplicantInfoCompleted = true;

    }

    public Residence AddResidence(
        string address,
        string landlordName,
        string landlordPhone,
        DateOnly moveIn,
        DateOnly moveOut)
    {
        CheckEditable();

        var residence = new Residence();

        SetResidence(
            residence,
            address,
            landlordName,
            landlordPhone,
            moveIn,
            moveOut);

        Residences.Add(residence);

        ResidenceHistoryCompleted = false;

        return residence;

    }

    public void UpdateResidence(
        int residenceId,
        string address,
        string landlordName,
        string landlordPhone,
        DateOnly moveIn,
        DateOnly moveOut)
    {
        CheckEditable();

        SetResidence(
            GetResidence(residenceId),
            address,
            landlordName,
            landlordPhone,
            moveIn,
            moveOut);

        ResidenceHistoryCompleted = false;
    }

    public void RemoveResidence(int residenceId)
    {
        CheckEditable();

        Residences.Remove(GetResidence(residenceId));

        ResidenceHistoryCompleted = false;
    }

    public void SetResidenceHistoryCompleted()
    {
        CheckEditable();

        if (Residences.Count == 0)
            throw new RuleException("Please add at least one prior residence.");

        ResidenceHistoryCompleted = true;
    }

    public void Submit(
        string userId,
        DateTime nowUtc,
        DateOnly today,
        IEnumerable<Lease> unitLeases)
    {
        if (!CanSubmit)
            throw new RuleException("The application is not fully complete.");

        foreach (var existing in unitLeases.Where(l => l.UnitId == UnitId))
        {
            if (existing.IsActive(today))
                throw new RuleException("This unit already has an active lease.");
        }

        SubmittedAtUtc = nowUtc;

        SetStatus(ApplicationStatus.Submitted, userId, nowUtc);

    }

    public void Withdraw(string userId, DateTime nowUtc)
    {
        if (!CanWithdraw)
            throw new RuleException("This application cannot be withdrawn.");

        SetStatus(ApplicationStatus.Withdrawn, userId, nowUtc);
    }

    public Lease ApproveApplicationForLease(
        string userId,
        string? comment,
        DateTime nowUtc,
        DateOnly today,
        DateOnly leaseStartDate,
        IEnumerable<Lease> unitLeases)
    {
        CheckReviewable();

        if (leaseStartDate < today)
            throw new RuleException("The lease start date cannot be in the past.");

        var newLease = Lease.CreateFromApplication(this, leaseStartDate);

        foreach (var existing in unitLeases.Where(l => l.UnitId == UnitId))
        {
            if (existing.IsActive(today) || existing.Overlaps(newLease.StartDate, newLease.EndDate))
                throw new RuleException("This unit already has a lease for that period.");
        }

        SetStatus(
            ApplicationStatus.Approved,
            userId,
            nowUtc,
            comment);

        Lease = newLease;

        return Lease;
    }

    public void ReturnLease(
        string userId,
        string comment,
        DateTime nowUtc)
    {
        CheckReviewable();

        RequireComment(comment);

        SetStatus(
            ApplicationStatus.Returned,
            userId,
            nowUtc,
            comment);
    }

    public void DenyLease(
        string userId,
        string comment,
        DateTime nowUtc)
    {
        CheckReviewable();

        RequireComment(comment);

        SetStatus(
            ApplicationStatus.Denied,
            userId,
            nowUtc,
            comment);
    }

    private void CheckEditable()
    {
        if (!IsEditable)
            throw new RuleException($"An application that is {Status} cannot be edited.");
    }

    private void CheckReviewable()
    {
        if (!CanReview)        
            throw new RuleException("The application must be submitted before it can be reviewed.");
    }

    private void RequireComment(string? comment)
    {
        if (string.IsNullOrWhiteSpace(comment))
            throw new RuleException("A comment is required!");
    }

    private static void SetResidence(
        Residence residence,
        string address,
        string landlordName,
        string landlordPhone,
        DateOnly moveIn,
        DateOnly moveOut)
    {
        if (moveOut < moveIn)
            throw new RuleException("Move Out date cannot be before the Move In date.");

        residence.Address = address.Trim();

        residence.LandlordName = landlordName.Trim();

        residence.LandlordPhone = landlordPhone.Trim();

        residence.MoveInDate = moveIn;

        residence.MoveOutDate = moveOut;
    }

    private Residence GetResidence(int residenceId) =>
        Residences.SingleOrDefault(r => r.Id == residenceId)
        ?? throw new RuleException("Residence not found on this application.");

    private void SetStatus(
        ApplicationStatus status,
        string userId,
        DateTime nowUtc,
        string? comment = null)
    {
        StatusHistory.Add(new ApplicationStatusHistory
        {
            FromStatus = Status,
            ToStatus = status,
            ChangedById = userId,
            ChangedAtUtc = nowUtc,
            Comment = comment?.Trim()
        });

        Status = status;
    }

}
