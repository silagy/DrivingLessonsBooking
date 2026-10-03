# Claude Designer Prompt: Publications Dashboard States (#76)

Copy everything below the line into Claude Designer. Attach the existing WeekDrive mockup (the "Hebrew · admin dashboard (RTL)" frame) so the new frames reuse its components.

---

Extend the existing **WeekDrive** admin mockup with new frames of the **publications dashboard** (לוח מעקב / פרסומים). Keep the current design system exactly: same top bar and navigation, the same card style, stat chips, status pill, buttons, week grid and legend. Don't restyle anything that already exists. Only add the states below.

## Context

WeekDrive is a weekly demand-collection tool for a driving school. Once a week the admin publishes **one link** for all teachers, shares it in the students' WhatsApp group, and watches the dashboard while students submit their preferred lesson slots. The dashboard shows one teacher and one week at a time: a header, stat chips, and a Sunday–Friday grid of request counts per slot.

Three things are missing from today's dashboard, and these frames should show them:

1. While the week is **open**, the admin has no way to see or copy the student link, even though they keep sharing it during the week.
2. The admin can't **change the week** on the dashboard itself.
3. When the selected week **hasn't been prepared yet**, the page is empty below the header, with no explanation.

## Surface and language

- Desktop, 1440px wide, admin app.
- **Hebrew, RTL**: everything mirrors (Sunday is the right-most grid column, and actions sit at the left end of the header). Numbers, times and the link URL stay left-to-right inside the RTL layout.
- Also make the frame in step 1 in **English, LTR**, so both directions are covered.

## The header (same on every frame)

- **Title row:** the teacher's name is the title itself, shown as text with a small chevron (it's a borderless dropdown that switches teachers), then "- שבוע 41", then the status pill. For example: `המורה כהן ⌄ - שבוע 41` and the green pill `פתוח`.
- **Subtitle row:** the week range is also a borderless dropdown with a small chevron, `4 באוק׳ - 9 באוק׳ 2026 ⌄`, then a middle dot, then the window line, for example `ההגשה נסגרת יום שישי, 9 באוקטובר בשעה 14:00`.
- **Actions** (open week): `הארכת מועד` (text button), `רענון` (outlined), `הורדת אקסל` (primary).

## Frames to make

### 1. Open week, with the share link (Hebrew, and the same in English)

The existing open-week dashboard, with one addition: a **share-link box** between the stat chips row and the grid card.

- Title `קישור לשיתוף`.
- A read-only field with the URL, left-to-right, truncated with an ellipsis if it's long: `https://weekdrive.co.il/s/nct2W42ow4Bku4mItNzwY`.
- Beside the field, a button `העתקת קישור`.
- Below it, a hint in small muted text: `קישור אחד לכל המורים - נוצר בעת הפרסום. שתפו אותו בעצמכם, למשל בקבוצת הוואטסאפ של התלמידים.`
- Show a second version with a success toast `הקישור הועתק.` after the copy click.
- English labels: `Shareable link`, `Copy link`, `Link copied.`, and the hint `One link for all teachers - generated on publish. Share it yourself, e.g. in the students' WhatsApp group.`

The box should feel secondary to the grid: the admin glances at it and copies the link, but the grid is still the main content. Try one version as a full-width strip and one where the box sits on the same row as the stat chips (chips at the start, link at the end). Recommend one.

### 2. The week picker, open

The open-week frame with the subtitle's week dropdown expanded. The options list five weeks, upcoming first:

- `27 בספט׳ - 2 באוק׳ 2026`
- `4 באוק׳ - 9 באוק׳ 2026` (selected, with a checkmark)
- `11 באוק׳ - 16 באוק׳ 2026`
- `18 באוק׳ - 23 באוק׳ 2026`
- `25 באוק׳ - 30 באוק׳ 2026`

The list uses the normal body font size, not the small subtitle size. Also show the teacher dropdown in the title expanded (`המורה כהן`, `המורה לוי`, `המורה מזרחי`), so both dropdowns read as part of the same pattern.

### 3. A week that hasn't been prepared

The header shows the teacher and the week range picker, but no `- שבוע N`, no status pill and no action buttons, because there's no publication. Below the header, instead of chips and the grid, one card:

- Message: `השבוע הזה עדיין לא הוכן.`
- Primary button at the end of the card: `להכנת השבוע` (goes to the weekly-prep page).

It must read as "nothing here yet, and here's the next step", not as an error. No red, no warning icon. Show it at 1440px and once at 1024px.

### 4. Loading

The header, with a centered spinner where the content goes. It's the same space the empty-state card uses, so the switch between them doesn't jump.

## Realistic content

Teachers `המורה כהן`, `המורה לוי`, `המורה מזרחי`. Week 41, 4–9 October 2026. Stat chips `14 תלמידים הגישו`, `38 סה״כ בחירות`, `הגשה אחרונה 11:37`. The data-as-of stamp `הנתונים נכונים ל־11:42`. The grid uses the existing counts and blocked cells from the current mockup.

## Deliverable

Frames 1–4 as above, each labelled, plus a one-paragraph note recommending the share-link placement from frame 1 and saying why.
