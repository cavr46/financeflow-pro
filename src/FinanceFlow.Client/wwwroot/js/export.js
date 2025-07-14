window.downloadFile = (content, filename, contentType) => {
    const blob = new Blob([content], { type: contentType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.generatePDF = async (htmlContent, filename) => {
    try {
        // Create a new window for printing
        const printWindow = window.open('', '_blank');
        if (!printWindow) {
            alert('Please allow popups for this site to generate PDFs');
            return;
        }
        
        printWindow.document.write(htmlContent);
        printWindow.document.close();
        
        // Wait for content to load
        printWindow.onload = () => {
            setTimeout(() => {
                printWindow.print();
                printWindow.close();
            }, 500);
        };
        
    } catch (error) {
        console.error('Error generating PDF:', error);
        alert('Error generating PDF. Please try again.');
    }
};

// Alternative method using html2canvas and jsPDF (if libraries are available)
window.generateAdvancedPDF = async (htmlContent, filename) => {
    if (typeof html2canvas !== 'undefined' && typeof jsPDF !== 'undefined') {
        try {
            const tempDiv = document.createElement('div');
            tempDiv.innerHTML = htmlContent;
            tempDiv.style.position = 'absolute';
            tempDiv.style.left = '-9999px';
            tempDiv.style.top = '-9999px';
            tempDiv.style.width = '210mm'; // A4 width
            document.body.appendChild(tempDiv);
            
            const canvas = await html2canvas(tempDiv);
            const imgData = canvas.toDataURL('image/png');
            
            const pdf = new jsPDF();
            const imgWidth = 210;
            const pageHeight = 295;
            const imgHeight = (canvas.height * imgWidth) / canvas.width;
            let heightLeft = imgHeight;
            
            let position = 0;
            
            pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
            heightLeft -= pageHeight;
            
            while (heightLeft >= 0) {
                position = heightLeft - imgHeight;
                pdf.addPage();
                pdf.addImage(imgData, 'PNG', 0, position, imgWidth, imgHeight);
                heightLeft -= pageHeight;
            }
            
            pdf.save(filename);
            document.body.removeChild(tempDiv);
            
        } catch (error) {
            console.error('Error generating advanced PDF:', error);
            // Fallback to basic method
            window.generatePDF(htmlContent, filename);
        }
    } else {
        // Fallback to basic method
        window.generatePDF(htmlContent, filename);
    }
};