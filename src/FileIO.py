//--------------------------------------------------------------------//
// ファイル読み込み・書き込み

// サーバー上のFEMデータファイルを読み込む
// fileName - データファイル名
function readServerFemFile(fileName){
  var xhr=new XMLHttpRequest();
  try{
    xhr.open('GET',fileName,false);
  }
  catch(e){
    alert(e);
  }
  xhr.onreadystatechange=function(){
    if((xhr.readyState===4) &&
       ((xhr.status===0) || (xhr.status===200))){
      try{
      	readFemModel(xhr.responseText.split(/\r?\n/g));
      }
      catch(ex){
      	alert(ex);
      }
    }
  };
  xhr.send(null);
}

// ローカルファイルを読み込む
function readLocalFile() {
  var file=document.getElementById('localreadfile').files[0];
  if((file===null) || (file===undefined)){
    alert('ファイルが指定されていません');
    return;
  }
  var reader=new FileReader();
  reader.readAsText(file);
  reader.onload=function(e){
    try{
      readFemModel(reader.result.split(/\r?\n/g));
    }
    catch(ex){
      alert(ex);
    }
  };
  hideModalWindow(FILE_WINDOW);
}

// ローカルファイルを書き込む
function writeLocalFile() {
  var file=document.getElementById('localwritefile').value.trim();
  if(file===''){
    alert('ファイル名が空白です');
    return;
  }
  var s=model.toStrings().join('\n')+'\n';
  var blob=new Blob([s],{type:'text/plain'});
  if(window.navigator.msSaveBlob){		// IE専用
    window.navigator.msSaveBlob(blob,file);
  }
  else{						// その他のブラウザ
    var a=document.createElement('a');
    a.href=URL.createObjectURL(blob);
    a.target='_blank';
    a.download=file;
    a.click();
  }
  hideModalWindow(FILE_WINDOW);
}

// FEMデータを読み込む
// s - データ文字列のリスト
function readFemModel(s){
  model.clear();
  var mesh=model.mesh,bc=model.bc,res=[];
  for(var i=0;i<s.length;i++){
    var ss=s[i].trim().replace(/\t/g,' ').split(/\s+/);
    if(ss.length>0){
      var keyWord=ss[0].toLowerCase();
// 材料データ
      if((keyWord=='material') && (ss.length>7)){
      	model.materials.push
      	  (new Material(parseInt(ss[1]),parseFloat(ss[2]),
      	      	      	parseFloat(ss[3]),parseFloat(ss[5]),
      	      	      	parseFloat(ss[6]),parseFloat(ss[7])));
      }
// シェルパラメータ
      else if((keyWord=='shellparameter') && (ss.length>2)){
      	model.shellParams.push
      	  (new ShellParameter(parseInt(ss[1]),parseFloat(ss[2])));
      }
// 梁パラメータ
      else if((keyWord=='barparameter') && (ss.length>4)){
      	model.barParams.push(new BarParameter
      	  (parseInt(ss[1]),ss[2],ss.slice(3,ss.length)));
      }
// 局所座標系
      else if((keyWord=='coordinates') && (ss.length>10)){
      	model.coordinates.push(readCoordinates(ss));
      }
// 節点
      else if((keyWord=='node') && (ss.length>4)){
      	mesh.nodes.push(new FENode(parseInt(ss[1]),parseFloat(ss[2]),
      	      	      	      	   parseFloat(ss[3]),
      	      	      	      	   parseFloat(ss[4])));
      }
// 要素
      else if((keyWord=='bebarelement') && (ss.length>5)){
      	if(ss.length<8){
      	  mesh.elements.push(new BEBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2)));
      	}
      	else{
      	  mesh.elements.push(new BEBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2),
      	     new THREE.Vector3().set(parseFloat(ss[6]),
      	      	      	      	     parseFloat(ss[7]),
      	      	      	      	     parseFloat(ss[8]))));
      	}
      }
      else if((keyWord=='tbarelement') && (ss.length>5)){
      	if(ss.length<8){
      	  mesh.elements.push(new TBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2)));
      	}
      	else{
      	  mesh.elements.push(new TBarElement
      	    (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	     readVertex(ss,4,2),
      	     new THREE.Vector3().set(parseFloat(ss[6]),
      	      	      	      	     parseFloat(ss[7]),
      	      	      	      	     parseFloat(ss[8]))));
      	}
      }
      else if((keyWord=='trielement1') && (ss.length>6)){
      	mesh.elements.push(new TriElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	   readVertex(ss,4,3)));
      }
      else if((keyWord=='quadelement1') && (ss.length>7)){
      	mesh.elements.push(new QuadElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),parseInt(ss[3]),
      	   readVertex(ss,4,4)));
      }
      else if((keyWord=='tetraelement1') && (ss.length>6)){
      	mesh.elements.push(new TetraElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,4)));
      }
      else if((keyWord=='wedgeelement1') && (ss.length>8)){
      	mesh.elements.push(new WedgeElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,6)));
      }
      else if((keyWord=='hexaelement1') && (ss.length>10)){
      	mesh.elements.push(new HexaElement1
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,8)));
      }
      else if((keyWord=='hexaelement1wt') && (ss.length>10)){
      	mesh.elements.push(new HexaElement1WT
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,8)));
      }
      else if((keyWord=='tetraelement2') && (ss.length>12)){
      	mesh.elements.push(new TetraElement2
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,10)));
      }
      else if((keyWord=='wedgeelement2') && (ss.length>17)){
      	mesh.elements.push(new WedgeElement2
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,15)));
      }
      else if((keyWord=='hexaelement2') && (ss.length>22)){
      	mesh.elements.push(new HexaElement2
      	  (parseInt(ss[1]),parseInt(ss[2]),readVertex(ss,3,20)));
      }
// 境界条件
      else if((keyWord=='restraint') && (ss.length>7)){
      	var rest=readRestraint(ss);
      	if(rest!==null) bc.restraints.push(rest);
      }
      else if((keyWord=='load') && (ss.length>4)){
      	bc.loads.push(readLoad(ss));
      }
      else if((keyWord=='pressure') && (ss.length>3)){
      	bc.pressures.push
      	  (new Pressure(parseInt(ss[1]),ss[2].toUpperCase(),
      	      	      	parseFloat(ss[3])));
      }
      else if((keyWord=='temperature') && (ss.length>2)){
      	bc.temperature.push
      	  (new Temperature(parseInt(ss[1]),parseFloat(ss[2])));
      }
      else if((keyWord=='htc') && (ss.length>4)){
      	bc.htcs.push
      	  (new HeatTransferBound(parseInt(ss[1]),ss[2].toUpperCase(),
      	      	      	      	 parseFloat(ss[3]),parseFloat(ss[4])));
      }
// 計算結果
      else if((keyWord=='resulttype') && (ss.length>1)){
      	if(ss[1].toLowerCase()=='element'){
      	  model.result.type=ELEMENT_DATA;
      	}
      	else{
      	  model.result.type=NODE_DATA;
      	}
      }
      else if(((keyWord=='eigenvalue') && (ss.length>2)) ||
      	      ((keyWord=='displacement') && (ss.length>7)) ||
      	      ((keyWord=='strain1') && (ss.length>7)) ||
      	      ((keyWord=='stress1') && (ss.length>7)) ||
      	      ((keyWord=='strenergy1') && (ss.length>2)) ||
      	      ((keyWord=='strain2') && (ss.length>7)) ||
      	      ((keyWord=='stress2') && (ss.length>7)) ||
      	      ((keyWord=='strenergy2') && (ss.length>2)) ||
      	      ((keyWord=='temp') && (ss.length>2))){
      	res.push(ss);
      }
    }
  }
  model.init();
  initObject();
  if(res.length>0){
    readFemResult(res);
  }
}

// FEMの計算結果を読み込む
// s - データ文字列のリスト
function readFemResult(s){
  var map1=[],map2=[],ss=[],res=model.result,p,i;
  var nodes=model.mesh.nodes,elems=model.mesh.elements;
  for(i=0;i<nodes.length;i++){
    map1[nodes[i].label]=i;
  }
  if(res.type===ELEMENT_DATA){
    for(i=0;i<elems.length;i++){
      map2[elems[i].label]=i;
    }
  }
  else{
    map2=map1;
  }
  for(i=0;i<s.length;i++){
    var keyWord=s[i][0].toLowerCase();
    ss.length=0;
    for(var j=2;j<s[i].length;j++){
      ss[j-2]=parseFloat(s[i][j]);
    }
    if(keyWord=='eigenvalue'){
      var ev=new EigenValue(ss[0],s[i][1]);
      model.result.addEigenValue(ev);
      res=ev;
    }
    else if(keyWord=='displacement'){
      p=readDataPointer(s[i],map1);
      var d=new Vector3R(ss[0],ss[1],ss[2],ss[3],ss[4],ss[5]);
      res.displacement[p]=d;
      res.dispMax=Math.max(res.dispMax,d.magnitude());
      res.angleMax=Math.max(res.angleMax,d.magnitudeR());
    }
    else if(keyWord=='strain1'){
      p=readDataPointer(s[i],map2);
      model.result.strain1[p]=new Strain(ss);
    }
    else if(keyWord=='stress1'){
      p=readDataPointer(s[i],map2);
      model.result.stress1[p]=new Stress(ss);
    }
    else if(keyWord=='strenergy1'){
      p=readDataPointer(s[i],map2);
      res.sEnergy1[p]=ss[0];
    }
    else if(keyWord=='strain2'){
      p=readDataPointer(s[i],map2);
      model.result.strain2[p]=new Strain(ss);
    }
    else if(keyWord=='stress2'){
      p=readDataPointer(s[i],map2);
      model.result.stress2[p]=new Stress(ss);
    }
    else if(keyWord=='strenergy2'){
      p=readDataPointer(s[i],map2);
      res.sEnergy2[p]=ss[0];
    }
    else if(keyWord=='temp'){
      p=readDataPointer(s[i],map1);
      model.result.temperature[p]=ss[0];
      model.result.tempMax=Math.max(model.result.tempMax,ss[0]);
    }
  }
  model.result.calculated=true;
  if(model.result.eigenValue.length===0){
    resultView.setInitStatic();
    showInfo();
  }
  else{
    resultView.setInitEigen();
  }
}

// データポインタを獲得する
// ss - データ文字列
// map - ラベルマップ
function readDataPointer(ss,map){
  var p=parseInt(ss[1]);
  if(p in map){
    return map[p];
  }
  else{
    throw new Error('計算結果'+ss[0]+'の番号'+p+
      	      	    'は存在しません');
  }
}

// 節点番号を読み取る
// ss - 文字列配列
// is - 開始インデックス
// count - 節点数
function readVertex(ss,is,count){
  var vertex=[];
  for(var j=0;j<count;j++) vertex[j]=parseInt(ss[is+j]);
  return vertex;
}

// 局所座標系を読み込む
// ss - データ文字列配列
function readCoordinates(ss){
  var c=[[parseFloat(ss[2]),parseFloat(ss[3]),parseFloat(ss[4])],
      	 [parseFloat(ss[5]),parseFloat(ss[6]),parseFloat(ss[7])],
      	 [parseFloat(ss[8]),parseFloat(ss[9]),parseFloat(ss[10])]];
  for(var i=0;i<3;i++){
    var ci=c[i];
    var cf=ci[0]*ci[0]+ci[1]*ci[1]+ci[2]*ci[2];
    if(cf===0){
      throw new Error('座標系'+ss[2]+'の軸方向ベクトルが0です');
    }
    cf=1/Math.sqrt(cf);
    ci[0]*=cf;
    ci[1]*=cf;
    ci[2]*=cf;
  }
  return new Coordinates(parseInt(ss[1]),c[0][0],c[1][0],c[2][0],
      	      	      	 c[0][1],c[1][1],c[2][1],
      	      	      	 c[0][2],c[1][2],c[2][2]);
}

// 拘束条件を読み込む
// ss - データ文字列配列
function readRestraint(ss){
  var rx=(parseInt(ss[2])!==0);
  var ry=(parseInt(ss[4])!==0);
  var rz=(parseInt(ss[6])!==0);
  var x=parseFloat(ss[3]),y=parseFloat(ss[5]),z=parseFloat(ss[7]);
  var coords=null;
  if(ss.length<14){
    if(ss.length>8) coords=parseInt(ss[8]);
    if(!rx && !ry && !rz) return null;
    return new Restraint(parseInt(ss[1]),coords,rx,ry,rz,x,y,z);
  }
  else{
    if(ss.length>14) coords=parseInt(ss[14]);
    var rrx=(parseInt(ss[8])!==0);
    var rry=(parseInt(ss[10])!==0);
    var rrz=(parseInt(ss[12])!==0);
    if(!rx && !ry && !rz && !rrx && !rry && !rrz) return null;
    return new Restraint(parseInt(ss[1]),coords,rx,ry,rz,x,y,z,
      	      	      	 rrx,rry,rrz,parseFloat(ss[9]),
      	      	      	 parseFloat(ss[11]),parseFloat(ss[13]));
  }
}

// 荷重条件を読み込む
// ss - データ文字列配列
function readLoad(ss){
  var coords=null;
  if(ss.length<8){
    if(ss.length>5) coords=parseInt(ss[5]);
    return new Load(parseInt(ss[1]),coords,parseFloat(ss[2]),
      	      	    parseFloat(ss[3]),parseFloat(ss[4]));
  }
  else{
    if(ss.length>8) coords=parseInt(ss[8]);
    return new Load(parseInt(ss[1]),coords,
      	      	    parseFloat(ss[2]),parseFloat(ss[3]),
      	      	    parseFloat(ss[4]),parseFloat(ss[5]),
      	      	    parseFloat(ss[6]),parseFloat(ss[7]));
  }
}

// ファイル操作ウィンドウを表示する
function showFileWindow(){
  showModalWindow(FILE_WINDOW);
}

// ファイル操作を取り消す
function cancelFile(){
  hideModalWindow(FILE_WINDOW);
// ファイル選択を消去する
  var localfile=document.getElementById('localreadfile');
  localfile.parentNode.innerHTML=localfile.parentNode.innerHTML;
}
