/* Compose the canvas */
function MockApp() {
  return (
    <DesignCanvas>
      <DCSection id="guide" title="Review guide" subtitle="Weekly demand collection for Cohen Driving School - admin publishes a week, students rank slots via a WhatsApp link, the system emails an Excel. Pink callouts = assumptions to validate.">
        <DCArtboard id="legend" label="Assumptions A1–A9" width={700}>
          <LegendBoard></LegendBoard>
        </DCArtboard>
      </DCSection>

      <DCSection id="admin" title="Admin - desktop" subtitle="One user: the school owner. Replaces ~3 hours of WhatsApp + Excel every week.">
        <DCArtboard id="ad-login" label="1 · Login" width={1180} height={760}>
          <AdminLogin></AdminLogin>
        </DCArtboard>
        <DCArtboard id="ad-teachers" label="2 · Cars & teachers" width={1180} height={900}>
          <AdminTeachers></AdminTeachers>
        </DCArtboard>
        <DCArtboard id="ad-roster" label="3 · Student roster upload" width={1180} height={860}>
          <AdminRoster></AdminRoster>
        </DCArtboard>
        <DCArtboard id="ad-prep" label="4 · Weekly preparation" width={1180} height={820}>
          <AdminPrep></AdminPrep>
        </DCArtboard>
        <DCArtboard id="ad-publish" label="5 · Publish dialog" width={1180} height={760}>
          <AdminPublish></AdminPublish>
        </DCArtboard>
        <DCArtboard id="ad-dash" label="6 · Live dashboard - window open" width={1180} height={780}>
          <AdminDashboard></AdminDashboard>
        </DCArtboard>
        <DCArtboard id="ad-closed" label="6b · Window closed - reopen" width={1180} height={520}>
          <AdminClosed></AdminClosed>
        </DCArtboard>
        <DCArtboard id="ad-history" label="7 · Publications history" width={1180} height={760}>
          <AdminHistory></AdminHistory>
        </DCArtboard>
      </DCSection>

      <DCSection id="student" title="Student form - mobile (375px)" subtitle="No login. The link arrives on WhatsApp; identity is the typed national ID, matched against the roster.">
        <DCArtboard id="st-closed" label="1 · Window not open" width={375} height={620}>
          <SWinClosed></SWinClosed>
        </DCArtboard>
        <DCArtboard id="st-id-known" label="2a · National ID - recognised" width={375} height={640}>
          <SIdKnown></SIdKnown>
        </DCArtboard>
        <DCArtboard id="st-id-editing" label="2b · Returning - editing loaded" width={375} height={640}>
          <SIdEditing></SIdEditing>
        </DCArtboard>
        <DCArtboard id="st-id-unknown" label="2c · Unknown ID - dead end" width={375} height={640}>
          <SIdUnknown></SIdUnknown>
        </DCArtboard>
        <DCArtboard id="st-details" label="3 · Your details (display only)" width={375} height={640}>
          <SDetails></SDetails>
        </DCArtboard>
        <DCArtboard id="st-target" label="4 · Target count" width={375} height={640}>
          <STarget></STarget>
        </DCArtboard>
        <DCArtboard id="st-slots" label="5a · Slot picking" width={375} height={812}>
          <SSlots></SSlots>
        </DCArtboard>
        <DCArtboard id="st-sheet" label="5b · Pick details panel" width={375} height={812}>
          <SSlotSheet></SSlotSheet>
        </DCArtboard>
        <DCArtboard id="st-review" label="6a · Ranked review" width={375} height={812}>
          <SReview></SReview>
        </DCArtboard>
        <DCArtboard id="st-review-err" label="6b · Picks < target" width={375} height={640}>
          <SReviewErr></SReviewErr>
        </DCArtboard>
        <DCArtboard id="st-done" label="7a · Confirmation" width={375} height={620}>
          <SDone></SDone>
        </DCArtboard>
        <DCArtboard id="st-done-err" label="7b · Closed mid-submit" width={375} height={620}>
          <SDoneErr></SDoneErr>
        </DCArtboard>
      </DCSection>

      <DCSection id="excel" title="Excel output" subtitle="The end of the system - booking stays manual. Emailed on every window close, versioned.">
        <DCArtboard id="ex-email" label="Email - versioned delivery" width={640}>
          <ExEmail></ExEmail>
        </DCArtboard>
        <DCArtboard id="ex-summary" label='Sheet 1 · "Summary"' width={1040}>
          <ExSummary></ExSummary>
        </DCArtboard>
        <DCArtboard id="ex-detail" label='Sheet 2 · "Detail"' width={1280} height={600}>
          <ExDetail></ExDetail>
        </DCArtboard>
      </DCSection>

      <DCSection id="rtl" title="Hebrew - RTL" subtitle="Fully mirrored layouts, not just translated strings. Language toggle lives in the header on both surfaces.">
        <DCArtboard id="he-slots" label="בחירת משבצות - student" width={375} height={812}>
          <HeSlots></HeSlots>
        </DCArtboard>
        <DCArtboard id="he-dash" label="לוח מעקב - admin dashboard" width={1180} height={780}>
          <HeDashboard></HeDashboard>
        </DCArtboard>
      </DCSection>
    </DesignCanvas>
  );
}

ReactDOM.createRoot(document.getElementById('root')).render(<MockApp></MockApp>);
