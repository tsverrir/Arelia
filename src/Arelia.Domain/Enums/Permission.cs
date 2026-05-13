using System.ComponentModel.DataAnnotations;

namespace Arelia.Domain.Enums;

public enum Permission
{
    [Display(Name = "Manage People")]
    ManagePeople,

    [Display(Name = "Manage Activities")]
    ManageActivities,

    [Display(Name = "Manage Attendance")]
    ManageAttendance,

    [Display(Name = "RSVP on Behalf")]
    RsvpOnBehalf,

    [Display(Name = "Manage Charges")]
    ManageCharges,

    [Display(Name = "Manage Expenses")]
    ManageExpenses,

    [Display(Name = "Manage Documents")]
    ManageDocuments,

    [Display(Name = "View Attendance Reports")]
    ViewAttendanceReports,

    [Display(Name = "View Finance Reports")]
    ViewFinanceReports,

    [Display(Name = "View Membership Reports")]
    ViewMembershipReports,

    [Display(Name = "Organisation Settings")]
    OrgSettings,

    [Display(Name = "Backups")]
    Backups
}

