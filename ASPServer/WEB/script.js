var BASE_URL = 'http://localhost:5000';

function showClass() {
	var classId = document.getElementById('classId').value;
	var url = BASE_URL + '/api/statistics';
    
	// Request class statistics
	var xhr = new XMLHttpRequest();
	xhr.open('GET', url, true);
    xhr.responseType = 'json';
    
    xhr.onload = function() {
        var status = xhr.status;
        
        if (status == 200) {
			if (classId <= 0 || classId >= xhr.response.length)
				return;
			
			var html = ''
			for (var i = 0; i < xhr.response[classId]; ++i) {
				var url = BASE_URL + '/api/' + classId.value + '/' + i;
				html += '<img src="' + url + '"><br>';
			}
			
			var win = window.open("", "Class " + classId);
			win.document.body.innerHTML = html;
        }
    };
    
    xhr.send();
}

function showClasses() {
	var url = BASE_URL + '/api/statistics';
    
	// Request class statistics
	var xhr = new XMLHttpRequest();
	xhr.open('GET', url, true);
    xhr.responseType = 'json';
    
    xhr.onload = function() {
        var status = xhr.status;
        
        if (status == 200) {
			if (classId <= 0 || classId >= xhr.response.length)
				return;
			
			var html = '<table id="classStatistics" border="1"><tr><th>ClassId</th><th>Count</th></tr>';
			for (var i = 0; i < xhr.response.length; ++i)
				html += '<tr><th>' + i + '</th><th>' + xhr.response[i] + '</th></tr>';
			html += '</table>';
			
			var win = window.open("", "Class statistics");
			win.document.body.innerHTML = html;
        }
    };
    
    xhr.send();
}

async function submitFile() {
	var button = document.getElementById('submitFile');
	var result = document.getElementById('uploadResult');
	button.enabled = false;
	
	let file = document.getElementById("fileInput").files[0];
	result.innerHTML = '<img src="' + URL.createObjectURL(file) + '">';
	let xhr = new XMLHttpRequest();
	let formData = new FormData();

	const toBase64 = file => new Promise((resolve, reject) => {
		const reader = new FileReader();
		reader.readAsDataURL(file);
		reader.onload = () => resolve(reader.result);
		reader.onerror = error => reject(error);
	});

	//formData.append(await toBase64(file));             
    xhr.responseType = 'json';                   
	xhr.open("PUT", BASE_URL + '/api/matchresult');
    
	//const response = fetch(BASE_URL + '/api/matchresult', {
	//	method: 'PUT',
	//	headers: {
	//		'Content-Type': 'application/json'
	//	},
	//	body: JSON.stringify(await toBase64(file)) 
	//});
	//
	//await response.json().then((data) => {
	//	console.log(data);
	//});
	
    xhr.onload = function() {
        var status = xhr.status;
        
		button.enabled = true;
		
        if (status == 200) {
			result.innerHTML += '<p>ClassId: ' + xhr.response.classId + ', Count: ' + xhr.response.statistics + '</p>';
        } else
			result.innerHTML += '<p>Error</p>';
    };
	
	xhr.send(await toBase64(file));
}