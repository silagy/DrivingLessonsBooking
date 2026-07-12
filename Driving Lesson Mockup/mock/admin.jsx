/* Admin desktop screens */

const wkUnavail = ['sun-morning','tue-evening','wed-noon','fri-noon'];
const wkCounts = {
  'sun-noon':2,'sun-afternoon':5,'sun-evening':3,
  'mon-morning':1,'mon-noon':2,'mon-afternoon':4,'mon-evening':6,
  'tue-morning':0,'tue-noon':1,'tue-afternoon':5,
  'wed-morning':2,'wed-afternoon':6,'wed-evening':4,
  'thu-morning':1,'thu-noon':3,'thu-afternoon':7,'thu-evening':5,
  'fri-morning':4,
};

/* Shared Sun–Fri grid. mode: 'prep' | 'counts' */
function DeskGrid({mode, lang, cellH}) {
  const he = lang === 'he';
  const H = cellH || 64;
  const headCell = {fontFamily:mkC.display, fontWeight:700, fontSize:13.5, color:mkC.ink,
    textAlign:'center', paddingBottom:10};
  const labelCell = {display:'flex', flexDirection:'column', justifyContent:'center', gap:2, paddingInlineEnd:14};
  return (
    <div style={{display:'grid', gridTemplateColumns:'118px repeat(6, 1fr)', gap:8, fontFamily:mkC.text}}>
      <div></div>
      {MK_DAYS.map((d) => (
        <div key={d.id} style={headCell}>
          {he ? d.he : d.enFull}
          <div style={{fontWeight:400, fontFamily:mkC.text, fontSize:11, color:mkC.g4, marginTop:2}}>{d.date}</div>
        </div>
      ))}
      {MK_SLOTS.map((s) => (
        <React.Fragment key={s.id}>
          <div style={labelCell}>
            <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:13.5, color:mkC.ink}}>{he ? s.he : s.en}</div>
            <div style={{fontSize:11, color:mkC.g4, fontVariantNumeric:'tabular-nums'}}>{s.time}</div>
          </div>
          {MK_DAYS.map((d) => {
            const key = d.id + '-' + s.id;
            if (!mkSlotExists(d.id, s.id)) {
              return <div key={key} style={{height:H}}></div>;
            }
            const blocked = wkUnavail.indexOf(key) !== -1;
            if (blocked) {
              return (
                <div key={key} style={{height:H, borderRadius:6, background:mkC.stripes, border:'1px solid '+mkC.g5,
                  display:'flex', alignItems:'center', justifyContent:'center', boxSizing:'border-box',
                  fontSize:11, fontWeight:500, color:mkC.g4}}>
                  {he ? 'חסום' : 'Unavailable'}
                </div>
              );
            }
            if (mode === 'prep') {
              return (
                <div key={key} style={{height:H, borderRadius:6, background:'#fff', border:'1px solid '+mkC.g5,
                  boxShadow:mkC.shadow, display:'flex', alignItems:'center', justifyContent:'center',
                  boxSizing:'border-box', fontSize:11.5, color:mkC.g4, cursor:'pointer'}}>
                  {he ? 'פתוח' : 'Open'}
                </div>
              );
            }
            const n = wkCounts[key] || 0;
            return (
              <div key={key} style={{height:H, borderRadius:6, background:'#fff', border:'1px solid '+mkC.g5,
                boxShadow:mkC.shadow, display:'flex', flexDirection:'column', alignItems:'center',
                justifyContent:'center', gap:1, boxSizing:'border-box'}}>
                <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:21,
                  color:n===0?mkC.g4:mkC.ink, fontVariantNumeric:'tabular-nums'}}>{n}</div>
                <div style={{fontSize:9.5, color:mkC.g4, letterSpacing:'.04em', textTransform:'uppercase'}}>
                  {he ? 'בקשות' : (n===1?'request':'requests')}</div>
              </div>
            );
          })}
        </React.Fragment>
      ))}
    </div>
  );
}

function GridLegend({lang}) {
  const he = lang === 'he';
  const item = {display:'flex', alignItems:'center', gap:7, fontSize:12, color:mkC.g3};
  return (
    <div style={{display:'flex', gap:22, fontFamily:mkC.text}}>
      <div style={item}><span style={{width:14, height:14, borderRadius:4, background:'#fff', border:'1px solid '+mkC.g5, display:'inline-block'}}></span>{he?'פתוח':'Open'}</div>
      <div style={item}><span style={{width:14, height:14, borderRadius:4, background:mkC.stripes, border:'1px solid '+mkC.g5, display:'inline-block'}}></span>{he?'לא זמין':'Unavailable'}</div>
      <div style={item}><span style={{width:14, height:14, borderRadius:4, background:mkC.g7, display:'inline-block'}}></span>{he?'מחוץ לרשת (שישי אחה״צ/ערב, שבת)':'Outside grid (Fri PM, Saturday)'}</div>
    </div>
  );
}

const adCard = {background:'#fff', border:'1px solid '+mkC.g5, borderRadius:14, boxShadow:mkC.shadow};
const adH1 = {fontFamily:mkC.display, fontWeight:500, fontSize:26, color:mkC.ink, margin:0, letterSpacing:'-0.01em'};
const adSub = {fontSize:13.5, color:mkC.g3, marginTop:4};

/* 1 - Login */
function AdminLogin() {
  return (
    <div style={{fontFamily:mkC.text, background:mkC.g7, height:760, display:'flex',
      alignItems:'center', justifyContent:'center', position:'relative'}}>
      <div style={{position:'absolute', top:28, insetInlineStart:36}}><MkLogo size={30}></MkLogo></div>
      <div style={{position:'absolute', top:30, insetInlineEnd:36}}><MkLang value="EN" small={false}></MkLang></div>
      <div style={{...adCard, width:400, padding:'40px 40px 34px', boxSizing:'border-box'}}>
        <div style={{fontFamily:mkC.display, fontWeight:500, fontSize:28, color:mkC.ink, letterSpacing:'-0.01em'}}>Sign in</div>
        <div style={{fontSize:13.5, color:mkC.g3, marginTop:6, lineHeight:1.5}}>Admin access for Cohen Driving School.</div>
        <div style={{display:'flex', flexDirection:'column', gap:18, marginTop:28}}>
          <MkField label="Email" value="avi.cohen@cohendriving.co.il"></MkField>
          <MkField label="Password" value="••••••••••" hint="Forgot password? Contact support."></MkField>
        </div>
        <MkBtn full arrow style={{marginTop:28}}>SIGN IN</MkBtn>
      </div>
      <div style={{position:'absolute', bottom:26, left:0, right:0, textAlign:'center', fontSize:12, color:mkC.g4}}>
        One admin account - the school owner. Students never sign in.
      </div>
    </div>
  );
}

/* 2 - Teachers & cars */
function CarRow({name, kind, trans}) {
  return (
    <div style={{display:'flex', alignItems:'center', gap:12, padding:'11px 16px',
      background:mkC.g7, border:'1px solid '+mkC.g5, borderRadius:8}}>
      <div style={{width:34, height:34, borderRadius:8, background:mkC.steelLight, color:mkC.whale,
        display:'flex', alignItems:'center', justifyContent:'center'}}>
        <svg width="18" height="18" viewBox="0 0 20 20" fill="none"><path d="M3 12.5L4.3 8.2C4.6 7.2 5.5 6.5 6.6 6.5H13.4C14.5 6.5 15.4 7.2 15.7 8.2L17 12.5M3 12.5H17M3 12.5V15M17 12.5V15" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round"></path><circle cx="6.2" cy="12.5" r="0.4" fill="currentColor" stroke="currentColor"></circle><circle cx="13.8" cy="12.5" r="0.4" fill="currentColor" stroke="currentColor"></circle></svg>
      </div>
      <div>
        <div style={{fontSize:14, fontWeight:500, color:mkC.ink}}>{name}</div>
        <div style={{fontSize:12, color:mkC.g3}}>{kind}</div>
      </div>
      <span style={{marginInlineStart:'auto', fontFamily:mkC.display, fontWeight:800, fontSize:10, letterSpacing:'.07em',
        textTransform:'uppercase', padding:'4px 11px', borderRadius:999,
        background:trans==='Automatic'?'#EAF1FF':mkC.g5, color:trans==='Automatic'?mkC.skyDark:mkC.g2}}>{trans}</span>
    </div>
  );
}
function AdminTeachers() {
  return (
    <AdminShell active="teachers">
      <div style={{display:'flex', alignItems:'flex-end', justifyContent:'space-between'}}>
        <div><h2 style={adH1}>Teachers &amp; cars</h2><div style={adSub}>Scheduling is per teacher; the roster assigns each student a car, so transmission is known - never asked.</div></div>
        <MkBtn variant="secondary" small>ADD TEACHER</MkBtn>
      </div>
      <div style={{display:'grid', gridTemplateColumns:'1fr 1fr', gap:20, marginTop:22}}>
        <div style={{...adCard, padding:24}}>
          <div style={{display:'flex', alignItems:'center', gap:12}}>
            <div style={{width:42, height:42, borderRadius:999, backgroundImage:mkC.grad, color:'#fff',
              display:'flex', alignItems:'center', justifyContent:'center', fontFamily:mkC.display, fontWeight:700, fontSize:15}}>AC</div>
            <div>
              <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:17, color:mkC.ink}}>Teacher Cohen</div>
              <div style={{fontSize:12.5, color:mkC.g3}}>avi.cohen@cohendriving.co.il · owner / admin</div>
            </div>
            <MkBtn variant="tertiary" small style={{marginInlineStart:'auto'}}>Edit</MkBtn>
          </div>
          <div style={{display:'flex', flexDirection:'column', gap:8, marginTop:18}}>
            <CarRow name="Corolla White" kind="Sedan" trans="Automatic"></CarRow>
            <CarRow name="i20 Silver" kind="Hatchback" trans="Manual"></CarRow>
          </div>
          <MkNote n={7} style={{marginTop:16}}>Each roster row pins a student to one car → transmission is known. Students are never asked Automatic/Manual; the form just displays it.</MkNote>
        </div>
        <div style={{...adCard, padding:24, alignSelf:'start'}}>
          <div style={{display:'flex', alignItems:'center', gap:12}}>
            <div style={{width:42, height:42, borderRadius:999, background:mkC.steelLight, color:mkC.whale,
              display:'flex', alignItems:'center', justifyContent:'center', fontFamily:mkC.display, fontWeight:700, fontSize:15}}>DL</div>
            <div>
              <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:17, color:mkC.ink}}>Teacher Levi</div>
              <div style={{fontSize:12.5, color:mkC.g3}}>dudi.levi@cohendriving.co.il</div>
            </div>
            <MkBtn variant="tertiary" small style={{marginInlineStart:'auto'}}>Edit</MkBtn>
          </div>
          <div style={{display:'flex', flexDirection:'column', gap:8, marginTop:18}}>
            <CarRow name="Picanto Red" kind="Hatchback" trans="Automatic"></CarRow>
          </div>
          <div style={{fontSize:12, color:mkC.g4, marginTop:14, lineHeight:1.5}}>Levi's students all drive the Picanto - the roster carries the car, so the form shows "Automatic" read-only.</div>
        </div>
      </div>
    </AdminShell>
  );
}

/* 3 - Student roster upload */
function RosterStat({n, label, tone}) {
  const tones = {add:{fg:'#1E7A39', bg:mkC.greenBg, bd:'#CFEAD8'}, upd:{fg:mkC.skyDark, bg:'#EAF1FF', bd:'#D7E3FF'},
    deact:{fg:mkC.g3, bg:mkC.g6, bd:mkC.g5}, err:{fg:mkC.plum, bg:'rgba(186,0,129,.05)', bd:mkC.plumLight}};
  const t = tones[tone];
  return (
    <div style={{flex:1, border:'1px solid '+t.bd, background:t.bg, borderRadius:10, padding:'13px 16px'}}>
      <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:26, color:t.fg, fontVariantNumeric:'tabular-nums'}}>{n}</div>
      <div style={{fontSize:12, color:mkC.g3, marginTop:1}}>{label}</div>
    </div>
  );
}
function AdminRoster() {
  const rows = [
    ['Noa Mizrahi','205374188','054-2218830','Corolla White','Cohen','Updated'],
    ['Omer Biton','312456789','052-7741206','i20 Silver','Cohen','Added'],
    ['Daniel Peretz','301992475','053-8120044','i20 Silver','Cohen','Updated'],
    ['Maya Azulay','318840271','054-6650912','Corolla White','Cohen','Added'],
    ['Itai Shapira','209113578','050-3398217','Picanto Red','Levi','Updated'],
    ['Yael Ben-David','315572904','058-4471130','Picanto Red','Levi','Added'],
  ];
  const errs = [
    ['47','Lior Avrahami','-','Unknown car “Mazda 2” - not in any teacher’s fleet'],
    ['52','Gal Hadad','30119','Invalid national ID - must be 9 digits'],
    ['58','(blank)','277418022','Missing assigned teacher'],
  ];
  const th = {fontFamily:mkC.display, fontWeight:800, fontSize:10.5, letterSpacing:'.07em', textTransform:'uppercase',
    color:mkC.g4, textAlign:'left', padding:'9px 14px', borderBottom:'1px solid '+mkC.g5};
  const td = {fontSize:13, color:mkC.ink, padding:'11px 14px', borderBottom:'1px solid '+mkC.g6};
  return (
    <AdminShell active="roster">
      <div style={{display:'flex', alignItems:'flex-end', justifyContent:'space-between', gap:24}}>
        <div><h2 style={adH1}>Student roster</h2><div style={adSub}>Upload the CSV that binds each student to a teacher and car. Re-uploading upserts on national ID.</div></div>
        <MkBtn arrow>UPLOAD CSV…</MkBtn>
      </div>
      <div style={{...adCard, padding:'16px 20px', marginTop:20, display:'flex', alignItems:'center', gap:14}}>
        <div style={{width:38, height:38, borderRadius:8, background:mkC.g6, border:'1px solid '+mkC.g5, color:mkC.g3,
          display:'flex', alignItems:'center', justifyContent:'center', flex:'none'}}>
          <svg width="18" height="18" viewBox="0 0 20 20" fill="none"><path d="M4 13v2a1 1 0 001 1h10a1 1 0 001-1v-2M10 3v9m0-9L6.5 6.5M10 3l3.5 3.5" stroke="currentColor" strokeWidth="1.6" strokeLinecap="round" strokeLinejoin="round"></path></svg>
        </div>
        <div style={{minWidth:0}}>
          <div style={{fontSize:13.5, fontWeight:500, color:mkC.ink}}>students-2026-06.csv</div>
          <div style={{fontSize:12, color:mkC.g3}}>61 rows · uploaded just now · upsert on national ID</div>
        </div>
        <span style={{marginInlineStart:'auto', fontFamily:mkC.display, fontWeight:800, fontSize:10, letterSpacing:'.07em',
          textTransform:'uppercase', color:'#1E7A39', background:mkC.greenBg, border:'1px solid #CFEAD8', borderRadius:999, padding:'5px 12px'}}>Processed</span>
      </div>
      <div style={{display:'flex', gap:12, marginTop:14}}>
        <RosterStat n="22" label="added" tone="add"></RosterStat>
        <RosterStat n="34" label="updated" tone="upd"></RosterStat>
        <RosterStat n="2" label="deactivated (absent from file)" tone="deact"></RosterStat>
        <RosterStat n="3" label="failed rows" tone="err"></RosterStat>
      </div>
      <div style={{display:'grid', gridTemplateColumns:'1.55fr 1fr', gap:20, marginTop:14, alignItems:'start'}}>
        <div style={{...adCard, overflow:'hidden'}}>
          <div style={{padding:'13px 16px', borderBottom:'1px solid '+mkC.g5, display:'flex', alignItems:'center', gap:10}}>
            <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:14, color:mkC.ink}}>Imported students</div>
            <MkSeg options={['All','Cohen','Levi']} value="All" small style={{marginInlineStart:'auto'}}></MkSeg>
          </div>
          <table style={{width:'100%', borderCollapse:'collapse', fontFamily:mkC.text}}>
            <thead><tr>
              <th style={th}>Name</th><th style={th}>National ID</th><th style={th}>Phone</th>
              <th style={th}>Car</th><th style={th}>Teacher</th><th style={{...th, textAlign:'right'}}>Result</th>
            </tr></thead>
            <tbody>
              {rows.map((r, i) => (
                <tr key={i}>
                  <td style={{...td, fontWeight:500}}>{r[0]}</td>
                  <td style={{...td, fontVariantNumeric:'tabular-nums', letterSpacing:'.04em', color:mkC.g3}}>{r[1]}</td>
                  <td style={{...td, color:mkC.g3, fontVariantNumeric:'tabular-nums'}}>{r[2]}</td>
                  <td style={td}>{r[3]}</td>
                  <td style={td}>{r[4]}</td>
                  <td style={{...td, textAlign:'right'}}>
                    <span style={{fontFamily:mkC.display, fontWeight:800, fontSize:10, letterSpacing:'.05em', textTransform:'uppercase',
                      color:r[5]==='Added'?'#1E7A39':mkC.skyDark, background:r[5]==='Added'?mkC.greenBg:'#EAF1FF',
                      border:'1px solid '+(r[5]==='Added'?'#CFEAD8':'#D7E3FF'), borderRadius:999, padding:'3px 9px'}}>{r[5]}</span>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
          <div style={{padding:'10px 16px', fontSize:12, color:mkC.g4, background:mkC.g7}}>Showing 6 of 56 imported · grouped by teacher</div>
        </div>
        <div style={{...adCard, padding:0, overflow:'hidden', border:'1px solid '+mkC.plumLight}}>
          <div style={{padding:'13px 16px', borderBottom:'1px solid '+mkC.plumLight, background:'rgba(186,0,129,.05)',
            display:'flex', alignItems:'center', gap:9}}>
            <span style={{color:mkC.plum, display:'inline-flex'}}>
              <svg width="16" height="16" viewBox="0 0 20 20" fill="none"><path d="M10 6.5v4.2" stroke="currentColor" strokeWidth="1.9" strokeLinecap="round"></path><circle cx="10" cy="13.6" r="1.05" fill="currentColor"></circle><circle cx="10" cy="10" r="7.3" stroke="currentColor" strokeWidth="1.6"></circle></svg>
            </span>
            <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:14, color:mkC.plum}}>3 rows failed</div>
          </div>
          <div style={{padding:'6px 0'}}>
            {errs.map((e, i) => (
              <div key={i} style={{padding:'9px 16px', borderBottom:i<2?'1px solid '+mkC.g6:'none'}}>
                <div style={{display:'flex', alignItems:'baseline', gap:8}}>
                  <span style={{fontFamily:mkC.display, fontWeight:700, fontSize:11, color:mkC.g4}}>Row {e[0]}</span>
                  <span style={{fontSize:12.5, fontWeight:500, color:mkC.ink}}>{e[1]}</span>
                </div>
                <div style={{fontSize:12, color:mkC.plum, marginTop:3, lineHeight:1.4}}>{e[3]}</div>
              </div>
            ))}
          </div>
          <div style={{padding:'11px 16px', borderTop:'1px solid '+mkC.g5}}>
            <MkBtn variant="secondary" small full>DOWNLOAD ERROR REPORT</MkBtn>
          </div>
        </div>
      </div>
      <MkNote n={6} style={{marginTop:16, maxWidth:780}}>The roster is the single source of truth: it routes each student to their teacher and car, so one school-wide link works for everyone - no teacher picker on the student form.</MkNote>
    </AdminShell>
  );
}

/* 4 - Weekly preparation */
function AdminPrep() {
  return (
    <AdminShell active="prep" minH={820}>
      <div style={{display:'flex', alignItems:'flex-end', justifyContent:'space-between', gap:24}}>
        <div><h2 style={adH1}>Prepare a week</h2><div style={adSub}>All slots start Open - click a slot to mark it Unavailable.</div></div>
        <MkBtn arrow>PUBLISH WEEK…</MkBtn>
      </div>
      <div style={{display:'flex', gap:16, marginTop:20, alignItems:'flex-end'}}>
        <MkField label="Teacher" value="Teacher Cohen" type="select" style={{width:220}}></MkField>
        <MkField label="Week" value="Week 25 · Jun 14–19, 2026" type="select" style={{width:260}}></MkField>
        <div style={{marginInlineStart:'auto', paddingBottom:4}}><MkStatus status="Draft"></MkStatus></div>
      </div>
      <div style={{...adCard, padding:'22px 24px', marginTop:18}}>
        <DeskGrid mode="prep"></DeskGrid>
        <div style={{display:'flex', justifyContent:'space-between', alignItems:'center', marginTop:18}}>
          <GridLegend></GridLegend>
          <div style={{fontSize:12, color:mkC.g4}}>4 slots marked unavailable</div>
        </div>
      </div>
      <MkNote n={3} style={{marginTop:16, maxWidth:760}}>
        The grid is hardcoded: Sun–Thu have four windows (07–12 / 12–15 / 15–18 / 18–22), Friday has Morning + Noon only, and Saturday doesn't exist anywhere in the system.
      </MkNote>
    </AdminShell>
  );
}

/* 4 - Publish dialog */
function LifeStep({label, state}) {
  const cur = state === 'cur', done = state === 'done';
  return (
    <div style={{display:'flex', flexDirection:'column', alignItems:'center', gap:7, width:86}}>
      <div style={{width:cur?26:18, height:cur?26:18, borderRadius:999, boxSizing:'border-box',
        backgroundImage:cur?mkC.grad:'none', background:cur?undefined:(done?mkC.ocean:'#fff'),
        border:done||cur?'none':'3px solid '+mkC.steel,
        display:'flex', alignItems:'center', justifyContent:'center',
        boxShadow:cur?'0 4px 12px rgba(0,87,255,.3)':'none'}}>
        {done ? <MkCheck size={10} color="#fff"></MkCheck> : null}
      </div>
      <div style={{fontFamily:mkC.display, fontWeight:cur?700:500, fontSize:11.5, color:cur?mkC.ink:mkC.g4}}>{label}</div>
    </div>
  );
}
function AdminPublish() {
  return (
    <div style={{position:'relative'}}>
      <AdminShell active="prep"><div style={{height:560}}></div></AdminShell>
      <div style={{position:'absolute', inset:0, background:'rgba(18,20,24,.45)',
        display:'flex', alignItems:'center', justifyContent:'center'}}>
        <div style={{background:'#fff', borderRadius:14, boxShadow:mkC.overlay, width:560, padding:'30px 34px', boxSizing:'border-box', fontFamily:mkC.text}}>
          <div style={{fontFamily:mkC.display, fontWeight:500, fontSize:22, color:mkC.ink}}>Publish Week 25 - all teachers</div>
          <div style={{fontSize:13, color:mkC.g3, marginTop:5}}>One window for the whole school. The roster routes each student to their teacher - you share a single link.</div>
          <div style={{display:'grid', gridTemplateColumns:'1fr 1fr', gap:14, marginTop:22}}>
            <MkField label="Submissions open" value="Wed, Jun 10 · 18:00"></MkField>
            <MkField label="Submissions close" value="Fri, Jun 12 · 14:00"></MkField>
          </div>
          <div style={{fontSize:11.5, color:mkC.g4, marginTop:6}}>All times Asia/Jerusalem.</div>
          <div style={{marginTop:20, border:'1px solid '+mkC.g5, borderRadius:10, padding:'14px 16px', background:mkC.g7}}>
            <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:12.5, color:mkC.g2}}>Shareable link</div>
            <div style={{display:'flex', gap:10, alignItems:'center', marginTop:8}}>
              <div style={{flex:1, background:'#fff', border:'1px solid '+mkC.g5, borderRadius:6, padding:'9px 12px',
                fontSize:13, color:mkC.ink, fontVariantNumeric:'tabular-nums', overflow:'hidden', textOverflow:'ellipsis', whiteSpace:'nowrap'}}>
                weekdrive.app/s/cohen-school-w25
              </div>
              <MkBtn small>COPY LINK</MkBtn>
            </div>
            <div style={{fontSize:11.5, color:mkC.g4, marginTop:8}}>One link for all teachers - generated on publish. You share it yourself, e.g. in the students' WhatsApp group.</div>
          </div>
          <div style={{marginTop:24, paddingTop:18, borderTop:'1px solid '+mkC.g5}}>
            <div style={{display:'flex', alignItems:'flex-start', justifyContent:'center', position:'relative'}}>
              <div style={{position:'absolute', top:11, left:60, right:60, height:3, background:mkC.g5, borderRadius:2}}></div>
              <div style={{display:'flex', justifyContent:'space-between', width:'100%', position:'relative'}}>
                <LifeStep label="Draft" state="done"></LifeStep>
                <LifeStep label="Published" state="cur"></LifeStep>
                <LifeStep label="Open" state="next"></LifeStep>
                <LifeStep label="Closed" state="next"></LifeStep>
              </div>
            </div>
            <div style={{fontSize:11.5, color:mkC.g4, textAlign:'center', marginTop:6}}>Opens automatically Wed 18:00 · closes Fri 14:00</div>
          </div>
          <div style={{display:'flex', justifyContent:'flex-end', gap:10, marginTop:22}}>
            <MkBtn variant="tertiary">Cancel</MkBtn>
            <MkBtn arrow>PUBLISH</MkBtn>
          </div>
        </div>
      </div>
    </div>
  );
}

/* 5 - Live dashboard (open) */
function StatChip({n, label}) {
  return (
    <div style={{display:'flex', alignItems:'baseline', gap:8, padding:'10px 18px',
      background:'#fff', border:'1px solid '+mkC.g5, borderRadius:10, boxShadow:mkC.shadow}}>
      <span style={{fontFamily:mkC.display, fontWeight:700, fontSize:24, color:mkC.ink, fontVariantNumeric:'tabular-nums'}}>{n}</span>
      <span style={{fontSize:12.5, color:mkC.g3}}>{label}</span>
    </div>
  );
}
function AdminDashboard() {
  return (
    <AdminShell active="dash" minH={780}>
      <div style={{display:'flex', alignItems:'flex-start', justifyContent:'space-between', gap:24}}>
        <div>
          <div style={{fontFamily:mkC.display, fontWeight:800, fontSize:10.5, letterSpacing:'.1em', textTransform:'uppercase', color:mkC.g4, marginBottom:6}}>Week 25 · Jun 14–19 · one open window</div>
          <div style={{display:'flex', alignItems:'center', gap:14}}>
            <MkSeg options={['Teacher Cohen','Teacher Levi']} value="Teacher Cohen"></MkSeg>
            <MkStatus status="Open"></MkStatus>
          </div>
          <div style={adSub}>Submissions close Fri, Jun 12 · 14:00 (Asia/Jerusalem) · selector switches teachers within the same week</div>
        </div>
        <div style={{display:'flex', gap:10, alignItems:'center'}}>
          <MkBtn variant="tertiary" small>Extend deadline</MkBtn>
          <MkBtn variant="secondary" small>REFRESH</MkBtn>
          <MkBtn small arrow>DOWNLOAD EXCEL</MkBtn>
        </div>
      </div>
      <div style={{display:'flex', gap:12, marginTop:18, alignItems:'center'}}>
        <StatChip n="14" label="students submitted"></StatChip>
        <StatChip n="38" label="total picks"></StatChip>
        <StatChip n="11:37" label="last submission"></StatChip>
        <div style={{marginInlineStart:'auto', fontSize:12.5, color:mkC.g3, fontVariantNumeric:'tabular-nums'}}>
          Data as of <b>11:42</b> - refresh to update
        </div>
      </div>
      <div style={{...adCard, padding:'22px 24px', marginTop:14}}>
        <DeskGrid mode="counts" cellH={62}></DeskGrid>
        <div style={{display:'flex', justifyContent:'space-between', alignItems:'center', marginTop:16}}>
          <GridLegend></GridLegend>
          <div style={{fontSize:12, color:mkC.g4}}>Counts = requests per slot, any rank</div>
        </div>
      </div>
      <div style={{display:'flex', gap:14, marginTop:14}}>
        <MkNote n={4} style={{flex:1}}>No live updates by design - the grid refreshes only when you click Refresh. The "data as of" stamp makes staleness visible.</MkNote>
        <MkNote n={2} style={{flex:1}}>A Double session still counts as <b>1</b> in these cells. The Single/Double flag lives in the Excel detail sheet.</MkNote>
      </div>
    </AdminShell>
  );
}

/* 5b - Closed window */
function AdminClosed() {
  return (
    <AdminShell active="dash" minH={520}>
      <div style={{display:'flex', alignItems:'flex-start', justifyContent:'space-between', gap:24}}>
        <div>
          <div style={{display:'flex', alignItems:'center', gap:14}}>
            <h2 style={adH1}>Teacher Cohen - Week 25</h2>
            <MkStatus status="Closed"></MkStatus>
          </div>
          <div style={adSub}>Window closed Fri, Jun 12 · 14:00 · 16 students · 43 picks</div>
        </div>
        <div style={{display:'flex', gap:10}}>
          <MkBtn variant="secondary" small>REOPEN WINDOW…</MkBtn>
          <MkBtn small arrow>DOWNLOAD EXCEL (V1)</MkBtn>
        </div>
      </div>
      <div style={{...adCard, padding:'18px 22px', marginTop:20, display:'flex', gap:14, alignItems:'center'}}>
        <div style={{width:38, height:38, borderRadius:999, background:mkC.greenBg, color:'#1E7A39',
          display:'flex', alignItems:'center', justifyContent:'center', flex:'none'}}>
          <MkCheck size={16}></MkCheck>
        </div>
        <div style={{fontSize:13.5, color:mkC.g2, lineHeight:1.55}}>
          <b>Excel v1 emailed</b> to avi.cohen@cohendriving.co.il at 14:00 - subject "Week 25 Requests – Teacher Cohen – v1".
          The student link now shows a friendly "window closed" message instead of the form.
        </div>
      </div>
      <div style={{...adCard, padding:'18px 22px', marginTop:12, display:'flex', gap:14, alignItems:'center', borderStyle:'solid'}}>
        <div style={{flex:'none'}}><MkStatus status="Published" label="If reopened"></MkStatus></div>
        <div style={{fontSize:13, color:mkC.g3, lineHeight:1.55}}>
          Reopening sets a new close time, lets students edit again, and - on the next close - emails a fresh file as <b>v2</b>.
        </div>
      </div>
      <MkNote n={8} style={{marginTop:16, maxWidth:720}}>
        Every close event sends a new versioned Excel. Reopen + close again → v2, v3… so the inbox always has an authoritative latest file.
      </MkNote>
    </AdminShell>
  );
}

/* 6 - Publications history */
function AdminHistory() {
  const rows = [
    ['Week 25','Jun 14–19','Cohen','Wed 18:00 → Fri 14:00','Open','-', false],
    ['Week 24','Jun 7–12','Cohen','Wed 18:00 → Fri 14:00','Closed','v2', true],
    ['Week 24','Jun 7–12','Levi','Wed 18:00 → Fri 12:00','Closed','v1', true],
    ['Week 23','May 31–Jun 5','Cohen','Tue 19:00 → Fri 14:00','Closed','v1', true],
    ['Week 23','May 31–Jun 5','Levi','Wed 18:00 → Sat - extended → Sun 10:00','Closed','v3', true],
    ['Week 22','May 24–29','Cohen','Wed 18:00 → Fri 14:00','Closed','v1', true],
  ];
  const th = {fontFamily:mkC.display, fontWeight:800, fontSize:10.5, letterSpacing:'.08em', textTransform:'uppercase',
    color:mkC.g4, textAlign:'left', padding:'10px 14px', borderBottom:'1px solid '+mkC.g5};
  const td = {fontSize:13.5, color:mkC.ink, padding:'13px 14px', borderBottom:'1px solid '+mkC.g6, verticalAlign:'middle'};
  return (
    <AdminShell active="history">
      <div><h2 style={adH1}>Publications history</h2><div style={adSub}>Every published week, per teacher - with the last emailed Excel version.</div></div>
      <div style={{...adCard, marginTop:20, overflow:'hidden'}}>
        <table style={{width:'100%', borderCollapse:'collapse', fontFamily:mkC.text}}>
          <thead><tr>
            <th style={th}>Week</th><th style={th}>Teacher</th><th style={th}>Submission window</th>
            <th style={th}>Status</th><th style={th}>Last Excel</th><th style={{...th, textAlign:'right'}}></th>
          </tr></thead>
          <tbody>
            {rows.map((r, i) => (
              <tr key={i} style={{background:i===0?mkC.g7:'#fff'}}>
                <td style={td}><b style={{fontWeight:500}}>{r[0]}</b><span style={{color:mkC.g4, fontSize:12}}> · {r[1]}</span></td>
                <td style={td}>{r[2]}</td>
                <td style={{...td, color:mkC.g3, fontSize:12.5}}>{r[3]}</td>
                <td style={td}><MkStatus status={r[4]} small></MkStatus></td>
                <td style={td}>{r[5]==='-'
                  ? <span style={{color:mkC.g4}}>-</span>
                  : <span style={{fontFamily:mkC.display, fontWeight:700, fontSize:12, background:mkC.g6, border:'1px solid '+mkC.g5, borderRadius:999, padding:'3px 10px'}}>{r[5]}</span>}
                </td>
                <td style={{...td, textAlign:'right'}}>
                  <MkBtn variant="tertiary" small>{r[6] ? 'Re-download' : 'View dashboard'}</MkBtn>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div style={{display:'flex', gap:14, marginTop:16}}>
        <MkNote n={8} style={{maxWidth:430}}>Week 23 · Levi shows v3 - closed, reopened twice, three emails sent.</MkNote>
        <MkNote n={9} style={{maxWidth:430}}>The trail ends here. Booking and assignment stay manual - the system's job is done once the Excel is in the inbox.</MkNote>
      </div>
    </AdminShell>
  );
}

Object.assign(window, {
  DeskGrid, GridLegend, wkUnavail, wkCounts,
  AdminLogin, AdminTeachers, AdminRoster, AdminPrep, AdminPublish, AdminDashboard, AdminClosed, AdminHistory,
});
