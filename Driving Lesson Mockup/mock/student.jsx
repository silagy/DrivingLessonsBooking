/* Student mobile flow - 375px · national-ID + roster identity */

const stTitle = {fontFamily:mkC.display, fontWeight:700, fontSize:21, color:mkC.ink, lineHeight:1.25, letterSpacing:'-0.005em'};
const stBody = {fontSize:13.5, color:mkC.g3, lineHeight:1.55};
const stPad = {padding:'20px 16px 0', display:'flex', flexDirection:'column', gap:10};

const stPicks3 = [
  {key:'sun-afternoon', rank:1},
  {key:'mon-evening', rank:2},
  {key:'wed-afternoon', rank:3},
];
const stUnavail = wkUnavail; /* from admin.jsx */

/* Mobile slot chip */
function SlotChip({slot, state, rank, he}) {
  const base = {flex:1, minWidth:0, borderRadius:8, padding:'8px 4px 7px', textAlign:'center', position:'relative',
    boxSizing:'border-box', border:'1.5px solid '+mkC.g5, background:'#fff', cursor:'pointer'};
  let look = {};
  if (state === 'picked') look = {border:'1.5px solid '+mkC.sky, background:'#F2F7FF'};
  if (state === 'blocked') look = {background:mkC.stripes, cursor:'default'};
  const fg = state === 'blocked' ? mkC.g4 : mkC.ink;
  return (
    <div style={{...base, ...look}}>
      <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:11.5, color:state==='picked'?mkC.skyDark:fg}}>{he ? slot.he : slot.en}</div>
      <div style={{fontSize:9, color:mkC.g4, marginTop:2, fontVariantNumeric:'tabular-nums'}}>{slot.time}</div>
      {rank ? (
        <div style={{position:'absolute', top:-7, insetInlineEnd:-6, width:19, height:19, borderRadius:999,
          backgroundImage:mkC.grad, color:'#fff', fontFamily:mkC.display, fontWeight:800, fontSize:10.5,
          display:'flex', alignItems:'center', justifyContent:'center', boxShadow:'0 2px 6px rgba(0,87,255,.35)'}}>{rank}</div>
      ) : null}
    </div>
  );
}

function SlotDayList({picks, he}) {
  const pickMap = {};
  (picks || []).forEach((p) => { pickMap[p.key] = p.rank; });
  return (
    <div style={{display:'flex', flexDirection:'column', gap:11}}>
      {MK_DAYS.map((d) => (
        <div key={d.id}>
          <div style={{display:'flex', alignItems:'baseline', gap:6, marginBottom:5}}>
            <span style={{fontFamily:mkC.display, fontWeight:700, fontSize:12.5, color:mkC.ink}}>{he ? d.he : d.enFull}</span>
            <span style={{fontSize:10.5, color:mkC.g4}}>{d.date}</span>
            {d.id === 'fri' ? <span style={{fontSize:10, color:mkC.g4, marginInlineStart:'auto'}}>{he ? 'בוקר וצהריים בלבד' : 'morning & noon only'}</span> : null}
          </div>
          <div style={{display:'flex', gap:6}}>
            {MK_SLOTS.filter((s) => mkSlotExists(d.id, s.id)).map((s) => {
              const key = d.id + '-' + s.id;
              const state = stUnavail.indexOf(key) !== -1 ? 'blocked' : (pickMap[key] ? 'picked' : 'open');
              return <SlotChip key={key} slot={s} state={state} rank={pickMap[key]} he={he}></SlotChip>;
            })}
          </div>
        </div>
      ))}
    </div>
  );
}

/* 1 - Window closed / not yet open */
function SWinClosed() {
  return (
    <PhoneShell h={620} noSub>
      <div style={{...stPad, alignItems:'center', textAlign:'center', paddingTop:72}}>
        <div style={{width:56, height:56, borderRadius:999, background:mkC.g6, border:'1px solid '+mkC.g5,
          display:'flex', alignItems:'center', justifyContent:'center', color:mkC.g3}}>
          <svg width="24" height="24" viewBox="0 0 24 24" fill="none"><circle cx="12" cy="12" r="8.5" stroke="currentColor" strokeWidth="1.8"></circle><path d="M12 7.5V12L15 14" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"></path></svg>
        </div>
        <div style={{...stTitle, marginTop:8}}>Submissions aren't open yet</div>
        <div style={{...stBody, maxWidth:280}}>
          The form for <b>Week 25 (Jun 14–19)</b> opens
          <b> Wednesday, Jun 10 at 18:00</b> and closes <b>Friday, Jun 12 at 14:00</b>.
        </div>
        <div style={{...stBody, fontSize:12.5, color:mkC.g4}}>Come back through the same link - no need for a new one.</div>
      </div>
      <MFoot>
        <div style={{fontSize:11.5, color:mkC.g4, textAlign:'center'}}>After closing, this page shows the same message with the closed date.</div>
      </MFoot>
    </PhoneShell>
  );
}

/* 2a - National ID entry, recognised (roster match, no submission yet) */
function SIdKnown() {
  return (
    <PhoneShell h={660} noSub>
      <div style={stPad}>
        <MStep n={1} of={5}></MStep>
        <div style={stTitle}>Enter your national ID</div>
        <div style={stBody}>We match it to the school roster to load your details - no password needed.</div>
        <MkField label="National ID" value="312 456 789" hint="9 digits, as registered with the school" style={{marginTop:8}}></MkField>
        <div style={{border:'1px solid #CFEAD8', background:mkC.greenBg, borderRadius:10, padding:'12px 14px',
          display:'flex', gap:10, alignItems:'flex-start'}}>
          <span style={{color:'#1E7A39', marginTop:1}}><MkCheck size={15}></MkCheck></span>
          <div style={{fontSize:12.5, color:mkC.g2, lineHeight:1.5}}>
            <b>Found you - Omer Biton.</b> No submission yet for Week 25 - let's collect your availability for Teacher Cohen.
          </div>
        </div>
        <MkNote n={6}>The roster resolves the ID to one student record - name, teacher and car - with no teacher picker. One school-wide link, the roster routes everyone.</MkNote>
      </div>
      <MFoot><MkBtn full arrow>CONTINUE</MkBtn></MFoot>
    </PhoneShell>
  );
}

/* 2b - National ID entry, returning (existing submission loaded for editing) */
function SIdEditing() {
  return (
    <PhoneShell h={660} noSub>
      <div style={stPad}>
        <MStep n={1} of={5}></MStep>
        <div style={stTitle}>Enter your national ID</div>
        <div style={stBody}>We match it to the school roster to load your details - no password needed.</div>
        <MkField label="National ID" value="205 374 188" hint="9 digits, as registered with the school" style={{marginTop:8}}></MkField>
        <div style={{border:'1px solid #CFEAD8', background:mkC.greenBg, borderRadius:10, padding:'12px 14px',
          display:'flex', gap:10, alignItems:'flex-start'}}>
          <span style={{color:'#1E7A39', marginTop:1}}><MkCheck size={15}></MkCheck></span>
          <div style={{fontSize:12.5, color:mkC.g2, lineHeight:1.5}}>
            <b>Welcome back, Noa Mizrahi.</b> You already submitted 5 picks on Thursday, so we've loaded them.
            You can <b>edit your submission</b> until the window closes (Fri 14:00).
          </div>
        </div>
        <MkNote n={5}>Identity is national-ID-only, on purpose: anyone with the link who types Noa's ID can view and edit her picks. Self-service editing beats a password for this audience.</MkNote>
      </div>
      <MFoot><MkBtn full arrow>EDIT MY SUBMISSION</MkBtn></MFoot>
    </PhoneShell>
  );
}

/* 2b - National ID entry, unknown (not on roster → dead-end) */
function SIdUnknown() {
  return (
    <PhoneShell h={660} noSub>
      <div style={stPad}>
        <MStep n={1} of={5}></MStep>
        <div style={stTitle}>Enter your national ID</div>
        <div style={stBody}>We match it to the school roster to load your details.</div>
        <MkField label="National ID" value="208 991 047" error focus style={{marginTop:8}}></MkField>
        <div style={{border:'1.5px solid '+mkC.plumLight, background:'rgba(186,0,129,.05)', borderRadius:10, padding:'13px 14px',
          display:'flex', gap:10, alignItems:'flex-start'}}>
          <span style={{flex:'none', marginTop:1, color:mkC.plum}}>
            <svg width="17" height="17" viewBox="0 0 20 20" fill="none"><circle cx="10" cy="10" r="8" stroke="currentColor" strokeWidth="1.8"></circle><path d="M10 6.2V10.5" stroke="currentColor" strokeWidth="1.8" strokeLinecap="round"></path><circle cx="10" cy="13.6" r="1.05" fill="currentColor"></circle></svg>
          </span>
          <div style={{fontSize:12.5, color:mkC.g2, lineHeight:1.5}}>
            <b style={{color:mkC.plum}}>We don't have you on file.</b> This ID isn't on Cohen Driving School's roster,
            so there's nothing to submit. Please contact your school to be added.
          </div>
        </div>
        <MkNote n={5}>No self-registration: an ID that isn't on the uploaded roster simply can't submit. The roster is the single source of truth for who exists.</MkNote>
      </div>
      <MFoot>
        <MkBtn full disabled>CONTINUE</MkBtn>
        <div style={{fontSize:11, color:mkC.g4, textAlign:'center'}}>Continue stays locked until the ID matches the roster</div>
      </MFoot>
    </PhoneShell>
  );
}

/* 3 - Your details (display-only, resolved from roster) */
function SDetails() {
  const row = (label, value, accent) => (
    <div style={{display:'flex', alignItems:'center', justifyContent:'space-between', padding:'12px 0',
      borderBottom:'1px solid '+mkC.g6}}>
      <span style={{fontSize:12.5, color:mkC.g4}}>{label}</span>
      <span style={{fontFamily:mkC.display, fontWeight:700, fontSize:14, color:accent||mkC.ink}}>{value}</span>
    </div>
  );
  return (
    <PhoneShell h={660} sub="Week 25 · Jun 14–19">
      <div style={{...stPad, paddingTop:24}}>
        <MStep n={2} of={5}></MStep>
        <div style={stBody}>You are submitting availability for</div>
        <div style={{display:'flex', alignItems:'center', gap:13, marginTop:-2}}>
          <div style={{width:52, height:52, borderRadius:999, background:mkC.steelLight, color:mkC.whale, flex:'none',
            display:'flex', alignItems:'center', justifyContent:'center', fontFamily:mkC.display, fontWeight:700, fontSize:18}}>AC</div>
          <div style={{...stTitle, fontSize:26}}>Teacher Cohen</div>
        </div>
        <div style={{border:'1px solid '+mkC.g5, borderRadius:12, padding:'4px 16px', background:'#fff', boxShadow:mkC.shadow, marginTop:6}}>
          {row('Student', 'Noa Mizrahi')}
          {row('Transmission', 'Automatic', mkC.skyDark)}
          {row('Car', 'Corolla White')}
          <div style={{display:'flex', alignItems:'center', justifyContent:'space-between', padding:'12px 0'}}>
            <span style={{fontSize:12.5, color:mkC.g4}}>Week</span>
            <span style={{fontFamily:mkC.display, fontWeight:700, fontSize:14, color:mkC.ink}}>Jun 14–19 · 2026</span>
          </div>
        </div>
        <MkNote n={6}>No teacher selection, mismatch warning, or "this is not my teacher" step - the roster routes each ID to exactly one teacher. One school-wide link serves everyone.</MkNote>
        <MkNote n={7}>Transmission and car are read-only here - both come from the roster, so the form never asks. Cohen's two cars don't add a question.</MkNote>
      </div>
      <MFoot><MkBtn full arrow>CONTINUE</MkBtn></MFoot>
    </PhoneShell>
  );
}

/* 4 - Target count */
function STarget() {
  return (
    <PhoneShell h={640}>
      <div style={stPad}>
        <MStep n={3} of={5}></MStep>
        <div style={stTitle}>How many lessons do you want this week?</div>
        <div style={stBody}>You'll then pick at least that many slots, ranked by preference.</div>
        <div style={{display:'flex', alignItems:'center', justifyContent:'center', gap:22, marginTop:26}}>
          <div style={{width:48, height:48, borderRadius:999, border:'2px solid '+mkC.g5, color:mkC.g4,
            display:'flex', alignItems:'center', justifyContent:'center', fontSize:24, cursor:'pointer'}}>−</div>
          <div style={{fontFamily:mkC.display, fontWeight:500, fontSize:64, color:mkC.ink, width:80, textAlign:'center', fontVariantNumeric:'tabular-nums'}}>2</div>
          <div style={{width:48, height:48, borderRadius:999, border:'2px solid '+mkC.ink, color:mkC.ink,
            display:'flex', alignItems:'center', justifyContent:'center', fontSize:24, cursor:'pointer'}}>+</div>
        </div>
        <div style={{textAlign:'center', fontSize:12, color:mkC.g4}}>lessons · minimum 1</div>
      </div>
      <MFoot><MkBtn full arrow>PICK SLOTS</MkBtn></MFoot>
    </PhoneShell>
  );
}

/* 5a - Slot picking */
function SSlots() {
  return (
    <PhoneShell h={812}>
      <div style={{padding:'14px 16px 0'}}>
        <MStep n={4} of={5}></MStep>
        <div style={{...stTitle, fontSize:18, marginTop:4}}>Pick your slots, best first</div>
        <div style={{...stBody, fontSize:12, marginTop:2}}>Tap to add - order = preference. Tap a picked slot to edit or remove it.</div>
      </div>
      <div style={{padding:'12px 16px 8px', overflow:'hidden'}}>
        <SlotDayList picks={stPicks3}></SlotDayList>
      </div>
      <MFoot>
        <div style={{display:'flex', alignItems:'center', gap:8, fontSize:12.5, color:mkC.g2}}>
          <span><b>Target: 2</b> · Picked: 3</span>
          <span style={{color:'#1E7A39', display:'inline-flex'}}><MkCheck size={13}></MkCheck></span>
          <span style={{marginInlineStart:'auto', color:mkC.g4, fontSize:11.5}}>extra picks = backups</span>
        </div>
        <MkBtn full arrow>REVIEW MY LIST</MkBtn>
      </MFoot>
    </PhoneShell>
  );
}

/* 5b - Pick details bottom sheet */
function SSlotSheet() {
  return (
    <PhoneShell h={812}>
      <div style={{padding:'14px 16px 0'}}>
        <MStep n={4} of={5}></MStep>
        <div style={{...stTitle, fontSize:18, marginTop:4}}>Pick your slots, best first</div>
      </div>
      <div style={{padding:'12px 16px 8px'}}>
        <SlotDayList picks={stPicks3}></SlotDayList>
      </div>
      <div style={{position:'absolute', inset:0, background:'rgba(18,20,24,.4)'}}></div>
      <div style={{position:'absolute', left:0, right:0, bottom:0, background:'#fff', borderRadius:'16px 16px 0 0',
        boxShadow:mkC.overlay, padding:'14px 16px 18px'}}>
        <div style={{width:36, height:4, borderRadius:99, background:mkC.g5, margin:'0 auto 12px'}}></div>
        <div style={{display:'flex', alignItems:'center', gap:8}}>
          <div style={{width:22, height:22, borderRadius:999, backgroundImage:mkC.grad, color:'#fff',
            fontFamily:mkC.display, fontWeight:800, fontSize:11.5, display:'flex', alignItems:'center', justifyContent:'center'}}>4</div>
          <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:15, color:mkC.ink}}>Thursday · Evening</div>
          <div style={{fontSize:11.5, color:mkC.g4, marginInlineStart:'auto'}}>18:00–22:00</div>
        </div>
        <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:12, color:mkC.g2, marginTop:14, marginBottom:6}}>Session type</div>
        <div style={{display:'flex', gap:8}}>
          <div style={{flex:1, border:'2px solid '+mkC.sky, background:'#F2F7FF', borderRadius:8, padding:'10px 8px',
            textAlign:'center', fontSize:13, fontWeight:500, color:mkC.skyDark}}>Single</div>
          <div style={{flex:1, border:'1.5px solid '+mkC.g5, borderRadius:8, padding:'10px 8px',
            textAlign:'center', fontSize:13, color:mkC.g3}}>Double <span style={{fontSize:10.5, color:mkC.g4}}>(2×)</span></div>
        </div>
        <div style={{marginTop:12}}>
          <MkField label="Constraint for this slot (optional)" value="only after 19:30" hint="Free text - e.g. 'only after 16:00', 'pick me up from work'"></MkField>
        </div>
        <div style={{display:'flex', gap:10, marginTop:16}}>
          <MkBtn variant="tertiary" style={{flex:'none'}}>Cancel</MkBtn>
          <MkBtn full arrow style={{flex:1}}>ADD AS PICK #4</MkBtn>
        </div>
      </div>
    </PhoneShell>
  );
}

/* Review list row */
function PickRow({rank, title, time, type, note, top}) {
  return (
    <div style={{display:'flex', gap:10, alignItems:'flex-start', padding:'11px 12px',
      background:'#fff', border:'1.5px solid '+(top?mkC.sky:mkC.g5), borderRadius:10,
      boxShadow:mkC.shadow}}>
      <div style={{display:'flex', flexDirection:'column', gap:3, paddingTop:3, color:mkC.g4, flex:'none'}}>
        <svg width="12" height="14" viewBox="0 0 12 14" fill="currentColor"><circle cx="3" cy="2.5" r="1.4"></circle><circle cx="9" cy="2.5" r="1.4"></circle><circle cx="3" cy="7" r="1.4"></circle><circle cx="9" cy="7" r="1.4"></circle><circle cx="3" cy="11.5" r="1.4"></circle><circle cx="9" cy="11.5" r="1.4"></circle></svg>
      </div>
      <div style={{width:21, height:21, borderRadius:999, flex:'none', marginTop:1,
        backgroundImage:top?mkC.grad:'none', background:top?undefined:mkC.g5,
        color:top?'#fff':mkC.g3, fontFamily:mkC.display, fontWeight:800, fontSize:11,
        display:'flex', alignItems:'center', justifyContent:'center'}}>{rank}</div>
      <div style={{minWidth:0}}>
        <div style={{fontSize:13.5, fontWeight:500, color:mkC.ink}}>{title} <span style={{color:mkC.g4, fontWeight:400, fontSize:11.5}}>{time}</span></div>
        <div style={{display:'flex', gap:6, marginTop:4, flexWrap:'wrap'}}>
          <span style={{fontSize:10, fontFamily:mkC.display, fontWeight:800, letterSpacing:'.05em', textTransform:'uppercase',
            background:type==='Double'?'#EAF1FF':mkC.g6, color:type==='Double'?mkC.skyDark:mkC.g3,
            border:'1px solid '+mkC.g5, borderRadius:999, padding:'2px 8px'}}>{type}</span>
          {note ? <span style={{fontSize:11, color:mkC.g3, fontStyle:'italic'}}>"{note}"</span> : null}
        </div>
      </div>
    </div>
  );
}

/* 6a - Ranked review */
function SReview() {
  return (
    <PhoneShell h={812}>
      <div style={stPad}>
        <MStep n={5} of={5}></MStep>
        <div style={stTitle}>Your ranked list</div>
        <div style={{...stBody, fontSize:12.5}}>
          <b>Target: 2 · Picked: 5</b> - your top 2 are your preferred slots, the rest are backups. Drag to reorder.
        </div>
        <div style={{display:'flex', flexDirection:'column', gap:8, marginTop:4}}>
          <PickRow rank={1} top title="Sunday · Afternoon" time="15:00–18:00" type="Double" note="only after 16:00"></PickRow>
          <PickRow rank={2} top title="Monday · Evening" time="18:00–22:00" type="Single"></PickRow>
          <PickRow rank={3} title="Wednesday · Afternoon" time="15:00–18:00" type="Single"></PickRow>
          <PickRow rank={4} title="Thursday · Evening" time="18:00–22:00" type="Single" note="only after 19:30"></PickRow>
          <PickRow rank={5} title="Friday · Morning" time="07:00–12:00" type="Single"></PickRow>
        </div>
        <MkNote n={1}>One ranked list - no separate "primary/alternative" buckets. Backups are just ranks 3+.</MkNote>
      </div>
      <MFoot><MkBtn full arrow>SUBMIT</MkBtn></MFoot>
    </PhoneShell>
  );
}

/* 6b - Validation: picks < target */
function SReviewErr() {
  return (
    <PhoneShell h={640}>
      <div style={stPad}>
        <MStep n={5} of={5}></MStep>
        <div style={stTitle}>Your ranked list</div>
        <div style={{border:'1.5px solid '+mkC.plumLight, background:'rgba(186,0,129,.05)', borderRadius:10,
          padding:'11px 14px', fontSize:12.5, color:mkC.g2, lineHeight:1.5}}>
          <b style={{color:mkC.plum}}>Not enough picks.</b> Your target is <b>2 lessons</b> but you've only picked <b>1 slot</b>.
          Add at least one more so the teacher has options.
        </div>
        <div style={{display:'flex', flexDirection:'column', gap:8, marginTop:2}}>
          <PickRow rank={1} top title="Sunday · Afternoon" time="15:00–18:00" type="Double" note="only after 16:00"></PickRow>
        </div>
        <MkBtn full variant="secondary">ADD MORE SLOTS</MkBtn>
      </div>
      <MFoot>
        <MkBtn full disabled>SUBMIT</MkBtn>
        <div style={{fontSize:11, color:mkC.g4, textAlign:'center'}}>Submit unlocks at 2+ picks</div>
      </MFoot>
    </PhoneShell>
  );
}

/* 7a - Confirmation */
function SDone() {
  return (
    <PhoneShell h={620} noSub>
      <div style={{...stPad, alignItems:'center', textAlign:'center', paddingTop:64}}>
        <div style={{width:62, height:62, borderRadius:999, backgroundImage:mkC.grad, color:'#fff',
          display:'flex', alignItems:'center', justifyContent:'center', boxShadow:'0 8px 24px rgba(0,87,255,.3)'}}>
          <MkCheck size={26} color="#fff"></MkCheck>
        </div>
        <div style={{...stTitle, marginTop:10}}>Submitted</div>
        <div style={{...stBody, maxWidth:280}}>
          5 picks for Week 25 with Teacher Cohen. You can <b>edit anytime before Friday 14:00</b> -
          just reopen this link and enter your national ID.
        </div>
      </div>
      <MFoot>
        <MkBtn full variant="secondary">EDIT MY SUBMISSION</MkBtn>
      </MFoot>
    </PhoneShell>
  );
}

/* 7b - Window closed mid-submit */
function SDoneErr() {
  return (
    <PhoneShell h={620} noSub>
      <div style={{...stPad, alignItems:'center', textAlign:'center', paddingTop:64}}>
        <div style={{width:62, height:62, borderRadius:999, background:'rgba(186,0,129,.08)', color:mkC.plum,
          border:'1.5px solid '+mkC.plumLight, display:'flex', alignItems:'center', justifyContent:'center'}}>
          <svg width="26" height="26" viewBox="0 0 24 24" fill="none"><path d="M12 7.5V13" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round"></path><circle cx="12" cy="16.6" r="1.3" fill="currentColor"></circle><circle cx="12" cy="12" r="9" stroke="currentColor" strokeWidth="1.8"></circle></svg>
        </div>
        <div style={{...stTitle, marginTop:10}}>The window just closed</div>
        <div style={{...stBody, maxWidth:290}}>
          Submissions for Week 25 closed at <b>Friday 14:00</b>, while you had this page open.
          Your changes were <b>not saved</b>. If it's urgent, message Teacher Cohen on WhatsApp.
        </div>
        <div style={{...stBody, fontSize:12, color:mkC.g4, maxWidth:280}}>If the teacher reopens the window, this link will work again.</div>
      </div>
    </PhoneShell>
  );
}

Object.assign(window, {
  SlotChip, SlotDayList, PickRow, stPicks3,
  SWinClosed, SIdKnown, SIdEditing, SIdUnknown, SDetails,
  STarget, SSlots, SSlotSheet, SReview, SReviewErr, SDone, SDoneErr,
});
