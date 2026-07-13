/* Shared primitives - Comply365 tokens applied to a small-business tool */
const mkC = {
  ink:'#121418', sky:'#0057FF', skyDark:'#003293', skyLight:'#7BAEFF',
  ocean:'#00BBC7', oceanDark:'#007A82', plum:'#BA0081', plumLight:'#FFA3E3',
  whale:'#005389', steel:'#C3CFE5', steelLight:'#DBE3F3',
  g2:'#2C2F37', g3:'#52555C', g4:'#9FA9BB', g5:'#E6E8F6', g6:'#F5F6FF', g7:'#FBFBFF',
  green:'#289E49', greenBg:'#E9F5EC',
  grad:'linear-gradient(135deg,#0057FF 0%,#00BBC7 100%)',
  shadow:'0 1px 2px rgba(18,20,24,.06), 0 2px 8px rgba(18,20,24,.06)',
  lift:'0 4px 16px rgba(18,20,24,.10), 0 1px 3px rgba(18,20,24,.06)',
  overlay:'0 20px 48px rgba(18,20,24,.18)',
  display:"'Helvetica Now Display','Helvetica Neue',Arial,'Liberation Sans',sans-serif",
  text:"'Helvetica Now Text','Helvetica Neue',Arial,'Liberation Sans',sans-serif",
  stripes:'repeating-linear-gradient(45deg,#F0F1F8 0px,#F0F1F8 6px,#E8EAF4 6px,#E8EAF4 12px)',
};

const MK_DAYS = [
  {id:'sun', en:'Sun', enFull:'Sunday',    he:'ראשון', date:'14/6'},
  {id:'mon', en:'Mon', enFull:'Monday',    he:'שני',   date:'15/6'},
  {id:'tue', en:'Tue', enFull:'Tuesday',   he:'שלישי', date:'16/6'},
  {id:'wed', en:'Wed', enFull:'Wednesday', he:'רביעי', date:'17/6'},
  {id:'thu', en:'Thu', enFull:'Thursday',  he:'חמישי', date:'18/6'},
  {id:'fri', en:'Fri', enFull:'Friday',    he:'שישי',  date:'19/6'},
];
const MK_SLOTS = [
  {id:'morning',   en:'Morning',   he:'בוקר',   time:'07:00–12:00'},
  {id:'noon',      en:'Noon',      he:'צהריים', time:'12:00–15:00'},
  {id:'afternoon', en:'Afternoon', he:'אחה״צ',  time:'15:00–18:00'},
  {id:'evening',   en:'Evening',   he:'ערב',    time:'18:00–22:00'},
];
const mkSlotExists = (dayId, slotId) => !(dayId === 'fri' && (slotId === 'afternoon' || slotId === 'evening'));

/* Brand arrow - hairline, not a unicode glyph */
function MkArrow({size}) {
  const s = size || 16;
  return (
    <svg width={s} height={s} viewBox="0 0 20 20" fill="none" style={{display:'block'}}>
      <path d="M3.5 10h12.5M11.5 5L16.5 10L11.5 15" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"></path>
    </svg>
  );
}
function MkCheck({size, color}) {
  const s = size || 16;
  return (
    <svg width={s} height={s} viewBox="0 0 20 20" fill="none" style={{display:'block'}}>
      <path d="M4 10.5L8.5 15L16 5.5" stroke={color || 'currentColor'} strokeWidth="2.4" strokeLinecap="round" strokeLinejoin="round"></path>
    </svg>
  );
}

/* Pill buttons per brand micro-rules */
function MkBtn({variant, children, disabled, full, small, style, arrow}) {
  const v = variant || 'primary';
  const base = {
    fontFamily:mkC.text, fontWeight:500, fontSize:small?12.5:14.5, lineHeight:1.2,
    letterSpacing:'.04em', padding:small?'8px 18px':'13px 26px', borderRadius:999,
    display:'inline-flex', alignItems:'center', justifyContent:'center', gap:9,
    cursor:disabled?'default':'pointer', boxSizing:'border-box',
    width:full?'100%':'auto', whiteSpace:'nowrap', userSelect:'none',
  };
  const looks = {
    primary:  {backgroundImage:mkC.grad, color:'#fff', border:'2px solid transparent'},
    secondary:{background:'transparent', color:mkC.ink, border:'2px solid '+mkC.ink},
    tertiary: {background:'transparent', color:mkC.sky, border:'2px solid transparent', padding:small?'8px 4px':'13px 6px'},
    quiet:    {background:mkC.g6, color:mkC.ink, border:'1px solid '+mkC.g5, fontWeight:500},
    danger:   {background:'transparent', color:mkC.plum, border:'2px solid '+mkC.plumLight},
  };
  const dis = disabled ? {opacity:.4} : {};
  return (
    <div style={{...base, ...looks[v], ...dis, ...style}}>
      <span>{children}</span>
      {arrow ? <MkArrow size={small?14:16}></MkArrow> : null}
    </div>
  );
}

/* Static input field */
function MkField({label, value, placeholder, hint, focus, error, style, dir, type}) {
  const bd = error ? mkC.plum : (focus ? mkC.sky : mkC.g5);
  return (
    <div style={{fontFamily:mkC.text, ...style}} dir={dir}>
      <div style={{fontSize:12.5, fontWeight:700, fontFamily:mkC.display, color:mkC.g2, marginBottom:6}}>{label}</div>
      <div style={{border:'1.5px solid '+bd, borderRadius:6, padding:'11px 14px', background:'#fff',
        fontSize:15, color:value?mkC.ink:mkC.g4, boxShadow:focus?'0 0 0 3px rgba(0,87,255,.12)':'none',
        display:'flex', alignItems:'center', justifyContent:'space-between', gap:8}}>
        <span>{value || placeholder}</span>
        {type==='select' ? <svg width="12" height="12" viewBox="0 0 12 12" fill="none"><path d="M2.5 4.5L6 8L9.5 4.5" stroke={mkC.g4} strokeWidth="1.8" strokeLinecap="round" strokeLinejoin="round"></path></svg> : null}
      </div>
      {hint ? <div style={{fontSize:11.5, color:error?mkC.plum:mkC.g4, marginTop:5, lineHeight:1.4}}>{hint}</div> : null}
    </div>
  );
}

/* Segmented control */
function MkSeg({options, value, small, style}) {
  return (
    <div style={{display:'inline-flex', background:mkC.g6, border:'1px solid '+mkC.g5, borderRadius:999, padding:3, gap:2, ...style}}>
      {options.map((o) => (
        <div key={o} style={{
          fontFamily:mkC.text, fontWeight:500, fontSize:small?11.5:13, padding:small?'5px 12px':'8px 20px',
          borderRadius:999, cursor:'pointer',
          background:o===value?'#fff':'transparent', color:o===value?mkC.ink:mkC.g3,
          boxShadow:o===value?mkC.shadow:'none', border:o===value?'1px solid '+mkC.g5:'1px solid transparent',
        }}>{o}</div>
      ))}
    </div>
  );
}

function MkLang({value, small}) {
  return <MkSeg options={['EN','עב']} value={value || 'EN'} small={small===undefined?true:small}></MkSeg>;
}

/* Status pill */
function MkStatus({status, label, small}) {
  const tones = {
    Draft:     {bg:mkC.g6,        fg:mkC.g3,      bd:mkC.g5},
    Published: {bg:'#EAF1FF',     fg:mkC.skyDark, bd:'#D7E3FF'},
    Open:      {bg:mkC.greenBg,   fg:'#1E7A39',   bd:'#CFEAD8'},
    Closed:    {bg:mkC.g5,        fg:mkC.g3,      bd:mkC.g5},
  };
  const t = tones[status] || tones.Draft;
  return (
    <span style={{fontFamily:mkC.display, fontWeight:800, fontSize:small?10:11, letterSpacing:'.08em',
      textTransform:'uppercase', color:t.fg, background:t.bg, border:'1px solid '+t.bd,
      borderRadius:999, padding:small?'3px 10px':'5px 13px', display:'inline-flex', alignItems:'center', gap:6}}>
      {status==='Open' ? <span style={{width:7, height:7, borderRadius:99, background:'#289E49', display:'inline-block'}}></span> : null}
      {label || status}
    </span>
  );
}

/* Assumption annotation callout (pink = "not product UI") */
function MkNote({n, children, style}) {
  return (
    <div style={{display:'flex', gap:10, alignItems:'flex-start', padding:'10px 14px',
      border:'1.5px dashed '+mkC.plum, borderRadius:10, background:'rgba(186,0,129,.035)',
      fontFamily:mkC.text, fontSize:12.5, lineHeight:1.5, color:mkC.g2, ...style}}>
      <span style={{flex:'none', fontFamily:mkC.display, fontWeight:800, fontSize:10.5, letterSpacing:'.06em',
        color:'#fff', background:mkC.plum, borderRadius:999, padding:'3px 9px', marginTop:1}}>A{n}</span>
      <span>{children}</span>
    </div>
  );
}

/* Product wordmark */
function MkLogo({size, name}) {
  const s = size || 26;
  return (
    <div style={{display:'flex', alignItems:'center', gap:9}}>
      <div style={{width:s, height:s, borderRadius:Math.round(s*.28), backgroundImage:mkC.grad,
        display:'flex', alignItems:'center', justifyContent:'center',
        fontFamily:mkC.display, fontWeight:800, fontSize:Math.round(s*.55), color:'#fff'}}>W</div>
      <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:Math.round(s*.62), color:mkC.ink, letterSpacing:'-0.01em'}}>{name || 'WeekDrive'}</div>
    </div>
  );
}

/* ===================== Admin desktop shell ===================== */
function AdminShell({active, children, rtl, minH}) {
  const nav = rtl
    ? [['teachers','רכבים ומורים'],['roster','רשימת תלמידים'],['prep','הכנת שבוע'],['dash','לוח מעקב'],['history','היסטוריה']]
    : [['teachers','Cars & teachers'],['roster','Roster'],['prep','Weekly prep'],['dash','Dashboard'],['history','History']];
  return (
    <div dir={rtl?'rtl':'ltr'} style={{fontFamily:mkC.text, background:mkC.g7, minHeight:minH||760, display:'flex', flexDirection:'column'}}>
      <div style={{background:'#fff', borderBottom:'1px solid '+mkC.g5, padding:'0 36px', height:62,
        display:'flex', alignItems:'center', gap:36}}>
        <MkLogo size={28}></MkLogo>
        <div style={{display:'flex', gap:26, alignItems:'center', height:'100%'}}>
          {nav.map(([id, label]) => (
            <div key={id} style={{height:'100%', display:'flex', alignItems:'center', position:'relative',
              fontFamily:mkC.text, fontSize:14.5, fontWeight:active===id?500:400,
              color:active===id?mkC.ink:mkC.g3, cursor:'pointer'}}>
              {label}
              {active===id ? <div style={{position:'absolute', bottom:0, left:0, right:0, height:3, backgroundImage:mkC.grad, borderRadius:'3px 3px 0 0'}}></div> : null}
            </div>
          ))}
        </div>
        <div style={{marginInlineStart:'auto', display:'flex', alignItems:'center', gap:16}}>
          <MkLang value={rtl?'עב':'EN'}></MkLang>
          <div style={{display:'flex', alignItems:'center', gap:9}}>
            <div style={{width:30, height:30, borderRadius:999, background:mkC.steelLight, color:mkC.whale,
              display:'flex', alignItems:'center', justifyContent:'center', fontFamily:mkC.display, fontWeight:700, fontSize:12}}>AC</div>
            <div style={{fontSize:13, color:mkC.g2}}>{rtl?'אבי כהן':'Avi Cohen'}</div>
          </div>
        </div>
      </div>
      <div style={{padding:'26px 36px 36px', flex:1, boxSizing:'border-box'}}>{children}</div>
    </div>
  );
}

/* ===================== Student mobile shell ===================== */
function PhoneShell({children, rtl, sub, h, noSub}) {
  return (
    <div dir={rtl?'rtl':'ltr'} style={{fontFamily:mkC.text, background:'#fff', height:h||812,
      display:'flex', flexDirection:'column', position:'relative', overflow:'hidden', boxSizing:'border-box'}}>
      <div style={{padding:'14px 16px 12px', borderBottom:'1px solid '+mkC.g5, display:'flex', alignItems:'center', gap:10}}>
        <div style={{width:26, height:26, borderRadius:8, backgroundImage:mkC.grad, color:'#fff',
          display:'flex', alignItems:'center', justifyContent:'center', fontFamily:mkC.display, fontWeight:800, fontSize:13}}>C</div>
        <div style={{fontFamily:mkC.display, fontWeight:700, fontSize:14.5, color:mkC.ink}}>
          {rtl ? 'כהן - בית ספר לנהיגה' : 'Cohen Driving School'}
        </div>
        <div style={{marginInlineStart:'auto'}}><MkLang value={rtl?'עב':'EN'}></MkLang></div>
      </div>
      {noSub ? null : (
        <div style={{padding:'7px 16px', background:mkC.g7, borderBottom:'1px solid '+mkC.g5,
          fontSize:11.5, color:mkC.g3}}>
          {sub || (rtl ? 'שבוע 25 · 14–19 ביוני · המורה כהן' : 'Week 25 · Jun 14–19 · Teacher Cohen')}
        </div>
      )}
      {children}
    </div>
  );
}

/* Sticky mobile footer */
function MFoot({children, style}) {
  return (
    <div style={{marginTop:'auto', borderTop:'1px solid '+mkC.g5, padding:'13px 16px',
      background:'#fff', display:'flex', flexDirection:'column', gap:9, ...style}}>{children}</div>
  );
}

/* Step caption */
function MStep({n, of, rtl}) {
  return <div style={{fontFamily:mkC.display, fontWeight:800, fontSize:10.5, letterSpacing:'.1em',
    textTransform:'uppercase', color:mkC.g4}}>{rtl ? `שלב ${n} מתוך ${of}` : `Step ${n} of ${of}`}</div>;
}

Object.assign(window, {
  mkC, MK_DAYS, MK_SLOTS, mkSlotExists,
  MkArrow, MkCheck, MkBtn, MkField, MkSeg, MkLang, MkStatus, MkNote, MkLogo,
  AdminShell, PhoneShell, MFoot, MStep,
});
