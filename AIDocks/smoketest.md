# Arelia Smoke Test Plan

## Prerequisites
- Application starts without errors
- Database is seeded (or fresh)

## Checklist

- [x] **1. Launch the application** — verify it starts without errors and the home page loads with the "Welcome to Arelia" heading.
- [x] **2. Verify unauthenticated state** — confirm the "Please sign in" message appears and the nav menu shows only Home and Calendar (no Management/Admin groups).
- [x] **3. Register a new account** — navigate to Account/Login → Register, create an account, and verify you land back authenticated.
- [x] **4. Verify authenticated home page** — confirm "Signed in as …" alert appears with the correct email.
- [x] **5. Toggle the nav drawer** — click the hamburger menu icon in the app bar; verify the drawer opens/closes.
- [x] **6. Verify nav menu items visible** — confirm Management (People, Activities, Roles, Finances) and Admin (Users, Organizations, Audit Log, Backups) groups appear.
- [x] **7. Create an organization** — navigate to Admin → Organizations, click "Create Organization", fill in a name, submit. Verify it appears in the table.
- [x] **8. Select the organization as active tenant** — switch to the new org (if tenant picker exists). Verify pages stop showing the "Please select an organization first" warning.
- [x] **9. Create a person** — navigate to People, click "Add Person", fill in first/last name + voice group, submit. Verify the person appears in the table.
- [x] **10. Search for the person** — type part of the name in the search box; verify the table filters in real time.
- [x] **11. Open person detail** — click the person row; verify the detail page loads with editable fields pre-populated.
- [x] **12. Edit the person** — change the phone number, click Save, verify the "Person updated" snackbar appears.
- [x] **13. Create a custom role** — navigate to Roles, click "Create Custom Role", enter a name, submit. Verify it appears in the All Roles table.
- [x] **14. Toggle a permission** — in the Permission Matrix, check a permission checkbox for the new role; verify it sticks on page reload.
- [x] **15. Assign the role to the person** — on the Person Detail page, select the new role, pick a From date, click Assign. Verify the assignment appears in the Role Assignments table.
- [x] **16. Create a Semester activity** — navigate to Activities, click "Create Activity", set type to Semester, fill dates spanning the current month, submit. Verify it appears in the activity list.
- [x] **17. Create a Rehearsal activity** — create another activity with type Rehearsal, nested date within the semester, submit. Verify it appears.
- [x] **18. View activity detail** — click a created activity; verify the detail page shows correct name, type, dates, and participant count of 0.
- [x] **19. Navigate back from activity detail** — click "Back to Activities"; verify you return to the activities list.
- [x] **20. Calendar view** — navigate to Calendar; verify the current month grid renders and the created activities appear as chips on their dates.
- [x] **21. Calendar navigation** — click the left/right chevron arrows and "Today" button; verify the month changes and returns.
- [x] **22. Click a calendar activity chip** — verify it navigates to the correct activity detail page.
- [x] **23. Record an expense** — navigate to Finances → Expenses tab, click "Record Expense", fill in description/amount/category/date, submit. Verify it appears in the Expenses table.
- [x] **24. Verify Charges tab** — switch to the Charges tab; verify it loads without error (empty is fine if no fees generated).
- [x] **25. Invite a user** — navigate to Admin → Users, click "Invite User", enter an email, submit. Verify the "Invitation sent" snackbar appears and the user appears in the table.
- [x] **26. Deactivate a user** — click the deactivate icon on the invited user row; confirm the dialog; verify status changes to "Former Member".
- [x] **27. View Audit Log** — navigate to Admin → Audit Log; verify entries appear reflecting the actions performed above (creates, updates).
- [x] **28. Create a backup** — navigate to Admin → Backups, click "Create Backup Now". Verify the "Backup created" snackbar appears and the backup appears in the table with file name, size, and timestamp.
- [x] **29. Deactivate a person** — on Person Detail, click "Deactivate"; confirm the dialog. Verify "Person deactivated" snackbar. Return to People list — verify the person no longer appears (inactive filter off).
- [x] **30. Show inactive people** — check the "Show inactive" checkbox; verify the deactivated person reappears with "Inactive" status chip.
- [x] **31. Notification bell** — click the notification bell in the app bar; verify the dropdown opens (shows "No notifications" or any generated notifications).
- [x] **32. Sign out** — click the person icon menu → Sign out; verify you return to the unauthenticated home page.
- [x] **33. Verify protected routes while signed out** — manually navigate to `/people`; verify you are redirected to the login page.

## Notes
<!-- Add comments referencing step numbers, e.g. "Step 14: permission did not persist — see issue #42" -->
- [x] 9. Voice group needs to be nullable. Not all members are singers. Add No voicegroup and Other options.
- [x] FAIL: 10. We need validation when creating the same person in a different org. If person already exists we should ask if you want to create a new person or assign (existing persion) to current org.
- [x] 10.1 We also need to be able to assign a person to org in the people page.
- [x] 11. We need to be able to deactivate a role. I.e. set valid to date after the fact. We also need to be able to delete all records if you have permission.
- [x] 11. On save (after updating field), the user should be routed to peope page.
- [x] 14. It sticks but... When I refresh the page I am prompted to select an organization. Seems that current org is not persisted. I can see an organization selected in the org selector, but menu options are like no org is selected.
- [x] We need to be able to specify the end date when ending role assignment for a person. It's very good to have the default today, but must be editable.
- [x] There must be possible to create an odd rehersal that is not a part of a schedule. I can do that by creating an activity, but I cannot have it marked as a rehersal.
- [x] Only persons that have the assigned role 'Member' should be automatically included in rehersals.
- [x] When I navigate Activities -> Activity detail, there is a button 'Back to semesters' which brings me to the semesters page. This button should bring me to the previous page which was Activities.
- [x] Expense categories should be managed the same way as voice groups are. You should be able to create- update- and delete categories, and select from them when recording an expense
- [x] 28. When I create a backup, I get the message: Backup failed: Database file not found.
- [ ] 
