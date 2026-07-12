/* Excel preview + assumptions legend + Hebrew RTL screens */

/* ---- Assumptions legend ---- */
const exAssumptions = [
  ['One ranked list + target count - no separate "primary/alternative" request types.'],
  ['A Double session counts as 1 in the summary grid; the Single/Double flag lives in the detail sheet.'],
  ['Sun–Fri grid, short Friday (morning + noon), no Saturday, four hardcoded slot windows.'],
  ['The dashboard refreshes manually - no live push updates, by design.'],
  ['National-ID identity: anyone with the link who types a roster member\'s national ID can view/edit that submission - an ID not on the roster cannot submit. Intentional.'],
  ['One school-wide weekly link - the roster routes each student to their teacher. No teacher selection, mismatch warning, or "this is not my teacher" escape hatch.'],
  ['Transmission and teacher come from the uploaded roster - never asked on the form.'],
  ['Every window close (including reopens) emails a new versioned Excel (v1, v2, v3…), per teacher.'],
  ['Booking/assignment is out of scope - the system ends at the Excel file.'],
];
function LegendBoard() {
  return (
    <div style={{fontFamily:mkC.text, background:'#fff', padding:'30px 32px', boxSizing:'border-box'}}>
      <div style={{fontFamily:mkC.display, fontWeight:800, fontSize:11, letterSpacing:'.1em', textTransform:'uppercase', color:mkC.plum}}>For client validation</div>
      <div style={{fontFamily:mkC.display, fontWeight:500, fontSize:25, color:mkC.ink, marginTop:6, letterSpacing:'-0.01em'}}>Nine assumptions baked into these screens</div>
      <div style={{fontSize:13, color:mkC.g3, marginTop:6, lineHeight:1.55}}>
        Each appears as a dashed pink callout on the screen that embodies it. Walk through them one by one - a "no" on any of these changes the build.
      </div>
      <div style={{display:'flex', flexDirection:'column', gap:9, marginTop:20}}>
        {exAssumptions.map((a, i) => (
          <div key={i} style={{display:'flex', gap:11, alignItems:'flex-start'}}>
            <span style={{flex:'none', fontFamily:mkC.display, fontWeight:800, fontSize:10.5, color:'#fff',
              background:mkC.plum, borderRadius:999, padding:'3px 9px', marginTop:1}}>A{i+1}</span>
            <span style={{fontSize:13, color:mkC.g2, lineHeight:1.5}}>{a[0]}</span>
          </div>
        ))}
      </div>
    </div>
  );
}

/* ---- Generic spreadsheet chrome ---- */
function SheetFrame({file, tab, children, width}) {
  const tabs = ['Summary', 'Detail'];
  return (
    <div style={{fontFamily:mkC.text, background:'#fff', boxSizing:'border-box'}}>
      <div style={{display:'flex', alignItems:'center', gap:10, padding:'10px 16px', borderBottom:'1px solid '+mkC.g5, background:mkC.g7}}>
        <div style={{width:22, height:22, borderRadius:5, background:'#1E7A39', color:'#fff', fontFamily:mkC.display,
          fontWeight:800, fontSize:11, display:'flex', alignItems:'center', justifyContent:'center'}}>X</div>
        <div style={{fontSize:13, fontWeight:500, color:mkC.ink}}>{file}</div>
        <div style={{fontSize:11.5, color:mkC.g4, marginLeft:'auto'}}>read-only preview</div>
      </div>
      <div style={{padding:'18px 16px 14px', overflow:'hidden'}}>{children}</div>
      <div style={{display:'flex', gap:2, padding:'0 16px', borderTop:'1px solid '+mkC.g5, background:mkC.g7}}>
        {tabs.map((t) => (
          <div key={t} style={{fontSize:12, fontWeight:t===tab?700:400, color:t===tab?mkC.ink:mkC.g3,
            background:t===tab?'#fff':'transparent', border:'1px solid '+mkC.g5, borderTop:t===tab?'2px solid #1E7A39':'1px solid '+mkC.g5,
            borderBottom:'none', padding:'6px 18px', borderRadius:'0 0 6px 6px', transform:'translateY(-1px)'}}>{t}</div>
        ))}
      </div>
    </div>
  );
}

const exTh = {fontFamily:mkC.display, fontWeight:700, fontSize:11.5, color:mkC.g2, background:mkC.g6,
  border:'1px solid '+mkC.g5, padding:'7px 10px', textAlign:'left'};
const exTd = {fontSize:12, color:mkC.ink, border:'1px solid '+mkC.g5, padding:'6px 10px', fontVariantNumeric:'tabular-nums'};

/* ---- Email ---- */
function ExEmail() {
  return (
    <div style={{fontFamily:mkC.text, background:'#fff', padding:'24px 26px', boxSizing:'border-box'}}>
      <div style={{display:'flex', flexDirection:'column', gap:7, fontSize:12.5, color:mkC.g3, borderBottom:'1px solid '+mkC.g5, paddingBottom:14}}>
        <div><span style={{display:'inline-block', width:62, color:mkC.g4}}>From</span> WeekDrive &lt;reports@weekdrive.app&gt;</div>
        <div><span style={{display:'inline-block', width:62, color:mkC.g4}}>To</span> avi.cohen@cohendriving.co.il</div>
        <div style={{fontSize:15, color:mkC.ink, fontWeight:700, fontFamily:mkC.display, marginTop:3}}>
          <span style={{display:'inline-block', width:62, color:mkC.g4, fontFamily:mkC.text, fontWeight:400, fontSize:12.5}}>Subject</span>
          Week 25 Requests – Teacher Cohen – v2
        </div>
      </div>
      <div style={{fontSize:13, color:mkC.g2, lineHeight:1.6, marginTop:16}}>
        The submission window for <b>Week 25 (Jun 14–19)</b> closed on Fri, Jun 12 at 16:30 after being reopened.<br></br>
        16 students · 43 picks. This file replaces v1.
      </div>
      <div style={{display:'inline-flex', alignItems:'center', gap:10, marginTop:16, border:'1px solid '+mkC.g5,
        borderRadius:10, padding:'10px 16px', background:mkC.g7}}>
        <div style={{width:30, height:30, borderRadius:6, background:'#1E7A39', color:'#fff', fontFamily:mkC.display,
          fontWeight:800, fontSize:13, display:'flex', alignItems:'center', justifyContent:'center'}}>X</div>
        <div>
          <div style={{fontSize:12.5, fontWeight:500, color:mkC.ink}}>week-25-cohen-v2.xlsx</div>
          <div style={{fontSize:11, color:mkC.g4}}>2 sheets · 18 KB</div>
        </div>
      </div>
      <MkNote n={8} style={{marginTop:18}}>v2 because the window was reopened and closed again - each close emails a fresh, fully regenerated file.</MkNote>
    </div>
  );
}

/* ---- Sheet 1: Summary ---- */
function ExSummary() {
  return (
    <SheetFrame file="week-25-cohen-v2.xlsx" tab="Summary">
      <table style={{borderCollapse:'collapse', width:'100%'}}>
        <thead>
          <tr>
            <th style={exTh}>Slot</th>
            {MK_DAYS.map((d) => <th key={d.id} style={{...exTh, textAlign:'center'}}>{d.enFull}</th>)}
          </tr>
        </thead>
        <tbody>
          {MK_SLOTS.map((s) => (
            <tr key={s.id}>
              <td style={{...exTd, fontWeight:500}}>{s.en} <span style={{color:mkC.g4, fontSize:10.5}}>{s.time}</span></td>
              {MK_DAYS.map((d) => {
                const key = d.id + '-' + s.id;
                if (!mkSlotExists(d.id, s.id)) return <td key={key} style={{...exTd, background:'#DDDFE9', border:'1px solid #D2D4E0'}}></td>;
                if (wkUnavail.indexOf(key) !== -1) return <td key={key} style={{...exTd, background:mkC.stripes, textAlign:'center', color:mkC.g4}}>blocked</td>;
                const n = wkCounts[key] || 0;
                return <td key={key} style={{...exTd, textAlign:'center', fontWeight:n>=5?700:400, background:n>=5?'#EAF1FF':'#fff'}}>{n}</td>;
              })}
            </tr>
          ))}
        </tbody>
      </table>
      <div style={{display:'flex', gap:14, marginTop:12}}>
        <MkNote n={2} style={{flex:1}}>Counts are requests per slot - a Double still counts as 1 here.</MkNote>
        <MkNote n={3} style={{flex:1}}>Friday Afternoon/Evening cells are absent (dark), not zero. No Saturday column exists.</MkNote>
      </div>
    </SheetFrame>
  );
}

/* ---- Sheet 2: Detail ---- */
function ExDetail() {
  const rows = [
    ['Sunday','Noon','Daniel Peretz','301992475','053-8120044','Manual','Single',1,2,''],
    ['Sunday','Afternoon','Noa Mizrahi','205374188','054-2218830','Automatic','Double',1,2,'only after 16:00'],
    ['Sunday','Afternoon','Maya Azulay','318840271','054-6650912','Automatic','Single',3,1,''],
    ['Monday','Morning','Itai Shapira','209113578','050-3398217','Manual','Single',1,1,'prefer 9–11'],
    ['Monday','Evening','Omer Biton','312456789','052-7741206','Automatic','Double',1,3,''],
    ['Monday','Evening','Noa Mizrahi','205374188','054-2218830','Automatic','Single',2,2,''],
    ['Tuesday','Noon','Shira Friedman','327650194','052-9007731','Automatic','Single',1,1,''],
    ['Wednesday','Afternoon','Daniel Peretz','301992475','053-8120044','Manual','Double',2,2,''],
    ['Wednesday','Afternoon','Noa Mizrahi','205374188','054-2218830','Automatic','Single',3,2,''],
    ['Thursday','Afternoon','Lior Avrahami','290447163','054-3320875','Manual','Single',1,2,'finishes school 15:30'],
    ['Thursday','Evening','Yael Ben-David','315572904','058-4471130','Automatic','Single',1,1,''],
    ['Friday','Morning','Omer Biton','312456789','052-7741206','Automatic','Single',2,3,'before 10:00 only'],
  ];
  const heads = ['Day','Slot','Student name','National ID','Phone','Transmission','Session type','Rank','Target','Constraints'];
  return (
    <SheetFrame file="week-25-cohen-v2.xlsx" tab="Detail">
      <table style={{borderCollapse:'collapse', width:'100%'}}>
        <thead><tr>{heads.map((h) => <th key={h} style={exTh}>{h}</th>)}</tr></thead>
        <tbody>
          {rows.map((r, i) => (
            <tr key={i}>
              {r.map((c, j) => (
                <td key={j} style={{...exTd,
                  textAlign:(j===7||j===8)?'center':'left',
                  color:(j===3||j===4)?mkC.g3:mkC.ink,
                  letterSpacing:j===3?'.04em':'normal',
                  fontStyle:j===9&&c?'italic':'normal'}}>{c===''?'':c}</td>
              ))}
            </tr>
          ))}
        </tbody>
      </table>
      <div style={{display:'flex', gap:14, marginTop:12, alignItems:'flex-start'}}>
        <div style={{fontSize:11.5, color:mkC.g4}}>Sorted by day → slot → rank. One row per pick.</div>
        <MkNote n={1} style={{marginLeft:'auto', maxWidth:420}}>Rank + Target replace "primary/alternative": Noa's target is 2, so her ranks 1–2 are preferred and rank 3 is a backup.</MkNote>
      </div>
    </SheetFrame>
  );
}

/* ---- Hebrew · student slot picking (RTL) ---- */
function HeSlots() {
  return (
    <PhoneShell rtl h={812}>
      <div style={{padding:'14px 16px 0'}}>
        <MStep n={4} of={5} rtl></MStep>
        <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:18, color:mkC.ink, marginTop:4}}>בחרו משבצות, הכי מתאים קודם</div>
        <div style={{fontSize:12, color:mkC.g3, lineHeight:1.55, marginTop:2}}>הקשה מוסיפה לרשימה - סדר הבחירה הוא סדר ההעדפה.</div>
      </div>
      <div style={{padding:'12px 16px 8px'}}>
        <SlotDayList picks={stPicks3} he></SlotDayList>
      </div>
      <MFoot>
        <div style={{display:'flex', alignItems:'center', gap:8, fontSize:12.5, color:mkC.g2}}>
          <span><b>יעד: 2</b> · נבחרו: 3</span>
          <span style={{color:'#1E7A39', display:'inline-flex'}}><MkCheck size={13}></MkCheck></span>
          <span style={{marginInlineStart:'auto', color:mkC.g4, fontSize:11.5}}>בחירות נוספות = גיבוי</span>
        </div>
        <MkBtn full>לרשימה המדורגת</MkBtn>
      </MFoot>
    </PhoneShell>
  );
}

/* ---- Hebrew · admin dashboard (RTL) ---- */
function HeStatChip({n, label}) {
  return (
    <div style={{display:'flex', alignItems:'baseline', gap:8, padding:'10px 18px',
      background:'#fff', border:'1px solid '+mkC.g5, borderRadius:10, boxShadow:mkC.shadow}}>
      <span style={{fontFamily:mkC.display, fontWeight:700, fontSize:24, color:mkC.ink, fontVariantNumeric:'tabular-nums'}}>{n}</span>
      <span style={{fontSize:12.5, color:mkC.g3}}>{label}</span>
    </div>
  );
}
function HeDashboard() {
  return (
    <AdminShell active="dash" rtl minH={780}>
      <div style={{display:'flex', alignItems:'flex-start', justifyContent:'space-between', gap:24}}>
        <div>
          <div style={{display:'flex', alignItems:'center', gap:14}}>
            <h2 style={{fontFamily:mkC.display, fontWeight:500, fontSize:26, color:mkC.ink, margin:0}}>המורה כהן - שבוע 25</h2>
            <MkStatus status="Open" label="פתוח"></MkStatus>
          </div>
          <div style={{fontSize:13.5, color:mkC.g3, marginTop:4}}>14–19 ביוני · ההגשה נסגרת ביום שישי, 12 ביוני · 14:00</div>
        </div>
        <div style={{display:'flex', gap:10, alignItems:'center'}}>
          <MkBtn variant="tertiary" small>הארכת מועד</MkBtn>
          <MkBtn variant="secondary" small>רענון</MkBtn>
          <MkBtn small>הורדת אקסל</MkBtn>
        </div>
      </div>
      <div style={{display:'flex', gap:12, marginTop:18, alignItems:'center'}}>
        <HeStatChip n="14" label="תלמידים הגישו"></HeStatChip>
        <HeStatChip n="38" label="בקשות סה״כ"></HeStatChip>
        <div style={{marginInlineStart:'auto', fontSize:12.5, color:mkC.g3}}>
          הנתונים נכונים ל־<b>11:42</b> - לחצו רענון לעדכון
        </div>
      </div>
      <div style={{background:'#fff', border:'1px solid '+mkC.g5, borderRadius:14, boxShadow:mkC.shadow, padding:'22px 24px', marginTop:14}}>
        <DeskGrid mode="counts" lang="he" cellH={62}></DeskGrid>
        <div style={{display:'flex', justifyContent:'space-between', alignItems:'center', marginTop:16}}>
          <GridLegend lang="he"></GridLegend>
          <div style={{fontSize:12, color:mkC.g4}}>ראשון מימין - הרשת מראה ימין→שמאל</div>
        </div>
      </div>
      <div style={{marginTop:14, display:'flex', gap:10, alignItems:'flex-start', padding:'10px 14px',
        border:'1.5px dashed '+mkC.plum, borderRadius:10, background:'rgba(186,0,129,.035)', fontSize:12.5, lineHeight:1.5, color:mkC.g2}}>
        <span style={{flex:'none', fontFamily:mkC.display, fontWeight:800, fontSize:10.5, color:'#fff', background:mkC.plum, borderRadius:999, padding:'3px 9px'}}>RTL</span>
        <span>פריסה מראה מלאה - ניווט, כפתורים, רשת הימים והערות מתחילים מימין. לא רק טקסט מתורגם.</span>
      </div>
    </AdminShell>
  );
}

Object.assign(window, { LegendBoard, ExEmail, ExSummary, ExDetail, HeSlots, HeDashboard });
