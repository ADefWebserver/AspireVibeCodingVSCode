// File download utility for Blazor WebAssembly
window.downloadFile = function (fileName, contentType, data) {
    const blob = new Blob([new Uint8Array(data)], { type: contentType });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    
    URL.revokeObjectURL(url);
};

// Download file from byte array (for RFP response documents)
window.downloadFileFromBytes = function (fileName, contentType, byteArray) {
    const blob = new Blob([byteArray], { type: contentType });
    const url = URL.createObjectURL(blob);
    
    const a = document.createElement('a');
    a.href = url;
    a.download = fileName;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    
    URL.revokeObjectURL(url);
};

// Virtual File System Access API for saving files locally
window.saveFileToLocal = async function (fileName, contentType, data) {
    if ('showSaveFilePicker' in window) {
        try {
            const fileExtension = fileName.split('.').pop();
            const fileHandle = await window.showSaveFilePicker({
                suggestedName: fileName,
                types: [{
                    description: 'Document files',
                    accept: {
                        [contentType]: [`.${fileExtension}`]
                    }
                }]
            });
            
            const writable = await fileHandle.createWritable();
            await writable.write(new Uint8Array(data));
            await writable.close();
            
            return true;
        } catch (err) {
            console.warn('File save cancelled or failed:', err);
            // Fallback to download
            window.downloadFile(fileName, contentType, data);
            return false;
        }
    } else {
        // Fallback to download for browsers that don't support File System Access API
        window.downloadFile(fileName, contentType, data);
        return false;
    }
};

// Enhanced RFP response download with File System Access API support
window.downloadRfpResponse = async function (fileName, byteArray, useFileSystemAPI = true) {
    const contentType = 'application/vnd.openxmlformats-officedocument.wordprocessingml.document';
    
    // Try File System Access API first if supported and requested
    if (useFileSystemAPI && 'showSaveFilePicker' in window) {
        try {
            const fileHandle = await window.showSaveFilePicker({
                suggestedName: fileName,
                types: [{
                    description: 'Word documents',
                    accept: {
                        'application/vnd.openxmlformats-officedocument.wordprocessingml.document': ['.docx']
                    }
                }]
            });
            
            const writable = await fileHandle.createWritable();
            await writable.write(byteArray);
            await writable.close();
            
            return { success: true, method: 'filesystem' };
        } catch (err) {
            console.warn('File System Access API failed, falling back to download:', err);
        }
    }
    
    // Fallback to regular download
    window.downloadFileFromBytes(fileName, contentType, byteArray);
    return { success: true, method: 'download' };
};