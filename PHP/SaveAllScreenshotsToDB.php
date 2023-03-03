<?php

$session = $_POST["session"];
$clientId = $_POST["clientId"];
$projectId = $_POST["projectId"];
$surfaces_ids = $_POST["ids"];
$screenshots = $_POST["screenshots"];

$nb_screenshots = count($screenshots);

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}

// Create the table
$tableCreation = "CREATE TABLE IF NOT EXISTS c" . $clientId . "_p" . $projectId . "_screenshots ( id_surface INT UNSIGNED NOT NULL, filename VARCHAR(50), session DATETIME, PRIMARY KEY (id_surface, session) ) CHARACTER SET 'utf8' ENGINE=INNODB;";
$result = $bdd->query($tableCreation);

if ($result->errorCode() == 00000) 
{
  echo "Screenshot table creation: OK\r\n";
} 
else 
{
  echo "Error while creating the screenshot table!\r\n";
}
$result->closeCursor();

//Insert screenshots
for($i = 0; $i < $nb_screenshots; $i++) {
	$insertCmd = "REPLACE INTO c" . $clientId . "_p" . $projectId . "_screenshots (id_surface, filename, session) VALUES ( '" . $surfaces_ids[$i] . "', '" . $screenshots[$i] . "', '" . $session . "');";
	echo $insertCmd;
	$result = $bdd->query($insertCmd);
	if ($result->errorCode() == 00000) {
  		echo "Screenshot insertion: OK\r\n";
	} 
	else {
  		echo "Error while inserting screenshot!\r\n";
  		break;
	}
}
$result->closeCursor();
?>