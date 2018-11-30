function getChart(dates, prices, vols, avgprice, avgvol, companyName) {
    document.getElementById("divChart").innerHTML = "";
    document.getElementById("divChart").innerHTML = '<canvas id="myChart" height="300" width="500"></canvas>';
    var ctx = document.getElementById("myChart").getContext('2d');
    ctx.canvas.width = 575;
    ctx.canvas.height = 350;
    if (myChart) myChart.destroy();
    var myChart = new Chart(ctx, {
        type: 'bar',
        data: {
            labels: dates.split(","),
            datasets: [{
                label: 'High Prices',
                yAxisID: 'H',
                data: prices.split(","),
                type: 'line',
                borderColor: 'rgba(0, 103, 71, 1)',
                backgroundColor: 'rgba(0, 103, 71, 0.1)',
                lineTension: 0
            },
            {
                label: 'Volumes (Mn)',
                data: vols.split(","),
                borderColor: '#337ab7',
                borderWidth: 1
            }]
        },
        options: {
            title: {
                display: true,
                text: companyName,
                fontColor: 'black'
            },
            responsive: false,
            scales: {
                yAxes: [{
                    id: 'H',
                    type: 'linear',
                    position: 'left',
                }]
            },
            annotation: {
                drawTime: 'afterDatasetsDraw',
                annotations: [
                    {
                        id: 'highprice',
                        type: 'line',
                        mode: 'horizontal',
                        scaleID: 'H',
                        value: avgprice,
                        borderColor: 'green',
                        borderWidth: 1,
                        label: {
                            backgroundColor: "green",
                            content: "Mean: $" + avgprice,
                            enabled: true
                        }
                    },
                    {
                        id: 'volume',
                        type: 'line',
                        mode: 'horizontal',
                        scaleID: 'H',
                        value: avgvol,
                        borderColor: '#337ab7',
                        borderWidth: 1,
                        label: {
                            backgroundColor: "#337ab7",
                            content: "Mean Volume: " + avgvol + "(Mn)",
                            enabled: true
                        }
                    }]
            }
        }
    });
}


