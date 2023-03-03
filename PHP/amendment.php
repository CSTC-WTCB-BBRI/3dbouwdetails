<?php
$clientId = $_GET["clientId"];
$projectId = $_GET["projectId"];
$session = $_GET["session"];

$lastSession = date_create_from_format('Y-m-d H:i:s', $session);
$lastSession->getTimestamp();

try
{
	$bdd = new PDO('mysql:host=localhost;dbname=tpdemo;charset=utf8', 'root', '', array(PDO::ATTR_ERRMODE => PDO::ERRMODE_EXCEPTION, PDO::MYSQL_ATTR_LOCAL_INFILE => true));
}
catch(Exception $e)
{
	die('Error : ' . $e->getMessage());
}
$query = "SELECT * FROM `c" . $clientId ."_p" . $projectId . "_avenant` ORDER BY session DESC;";
$result = $bdd->query($query);
?>

<!DOCTYPE html>
<html>
<head>
	<title>Avenant</title>
</head>
<link rel="stylesheet" type="text/css" href="myCSS.css">
<body>
	<h1>Avenant client <?php echo $clientId; ?>, projet <?php echo $projectId; ?></h1>
	<main id="accordion">
		<section id="visible-table">
			<a href="#visible-table"><h2>Dernières modifications</h2></a>
			<table style="background-color: lightblue; width: 100%">
				<colgroup>
					<col span="1" style="width: 10%;">
					<col span="1" style="width: 4%;">
					<col span="1" style="width: 20%;">
					<col span="1" style="width: 4%;">
					<col span="1" style="width: 5%;">
					<col span="1" style="width: 49%;">
					<col span="1" style="width: 8%;">
				</colgroup>
				<tr>
					<th>Etage</th>
					<th>Superficie</th>
					<th>Nom du carrelage</th>
					<th>Prix m²</th>
					<th>Prix surface</th>
					<th>Commentaires</th>
					<th>Session</th>
				</tr>
				<?php 
				$counter = 0;
				while($row = $result->fetch(PDO::FETCH_ASSOC)):;?>
					<?php $thisRowSession = date_create_from_format('Y-m-d H:i:s', $row['session']);
					$thisRowSession->getTimestamp();
					if ($thisRowSession == $lastSession):
						$counter += 1;
						;?>
						<tr style="background-color: lightgreen;">
							<td><?php echo $row['level'];?></td>
							<td><?php echo $row['area'];?></td>
							<td><?php echo $row['libelle'];?></td>
							<td><?php echo $row['unit_price'];?></td>
							<td><?php echo $row['total_price'];?></td>
							<td><?php echo $row['comment'];?></td>
							<td><?php echo $row['session'];?></td>
						</tr>
						<?php elseif ($counter == 0):;?>
							<tr>
								<td><?php echo "/";?></td>
								<td><?php echo "/";?></td>
								<td><?php echo "/";?></td>
								<td><?php echo "/";?></td>
								<td><?php echo "/";?></td>
								<td><?php echo "/";?></td>
								<td><?php echo "/";?></td>
							</tr>
						<?php endif;?>
					<?php endwhile;
					$result->closeCursor();?>
				</table>
			</section>
			<a href="#invisible-table"><h2>Anciennes modifications</h2></a>
			<section id="invisible-table">
				<table style="background-color: burlywood; width: 100%">
					<colgroup>
						<col span="1" style="width: 10%;">
						<col span="1" style="width: 4%;">
						<col span="1" style="width: 20%;">
						<col span="1" style="width: 4%;">
						<col span="1" style="width: 5%;">
						<col span="1" style="width: 49%;">
						<col span="1" style="width: 8%;">
					</colgroup>
					<tr>
						<th>Etage</th>
						<th>Superficie</th>
						<th>Nom du carrelage</th>
						<th>Prix m²</th>
						<th>Prix surface</th>
						<th>Commentaires</th>
						<th>Session</th>
					</tr>
					<?php 
					$result = $bdd->query($query);
					while($row = $result->fetch(PDO::FETCH_ASSOC)):;?>
						<?php $thisRowSession = date_create_from_format('Y-m-d H:i:s', $row['session']);
						$thisRowSession->getTimestamp();
						$counter = 0;
						if ($thisRowSession != $lastSession):
							$counter += 1;
							;?>
							<tr style="background-color: burlywood;">
								<td><?php echo $row['level'];?></td>
								<td><?php echo $row['area'];?></td>
								<td><?php echo $row['libelle'];?></td>
								<td><?php echo $row['unit_price'];?></td>
								<td><?php echo $row['total_price'];?></td>
								<td><?php echo $row['comment'];?></td>
								<td><?php echo $row['session'];?></td>
							</tr>
						<?php endif;?>
					<?php endwhile;?>
				</table>
			</section>
			<h1>Captures d'écran</h1>
			<section id="new-captures">
				<a href="#new-captures"><h2>Dernières modifications</h2></a>
				<?php
				$result->closeCursor();
				$newquery = "SELECT filename, comment, c" . $clientId ."_p" . $projectId . "_screenshots.session FROM `c" . $clientId ."_p" . $projectId . "_screenshots` LEFT JOIN `c" . $clientId . "_p" . $projectId . "_comments` ON (c" . $clientId ."_p" . $projectId . "_screenshots.id_surface=c" . $clientId . "_p" . $projectId . "_comments.id_surface) AND  (c" . $clientId ."_p" . $projectId . "_screenshots.session=c" . $clientId . "_p" . $projectId . "_comments.session) WHERE c" . $clientId ."_p" . $projectId . "_screenshots.session='" . $lastSession->format('Y-m-d H:i:s') . "' ORDER BY c" . $clientId . "_p" . $projectId . "_comments.session DESC;";
				$result = $bdd->query($newquery);
				?>
				<table>
					<?php
					$counter = 0;
					$totalCount = 0;
					$commentsArray = array();
					$sessionArray = array();
					while ($row = $result->fetch(PDO::FETCH_ASSOC)):
						$thisRowSession = date_create_from_format('Y-m-d H:i:s', $row['session']);
						$thisRowSession->getTimestamp();
						$totalCount += 1;
						if ($thisRowSession == $lastSession) {
							if ($counter == 0) {
								$commentsArray = array();
								$sessionArray = array();
								echo "<tr>";
							}
							array_push($commentsArray, $row['comment']);
							array_push($sessionArray, $row['session']);
							;?>
							<td>
								<div class="imageContainer">
									<img src=<?php echo "http://bimexpo/screenshots/" . $row['filename'] . " alt=\"" . $row['filename'] . "\"";?>  height="200" width="300">
								</div>
							</td>
							<?php
							if ($counter > 3 || $totalCount == $result->rowCount()) {
								echo "</tr>";
								echo "<tr>";
								foreach ($commentsArray as $value) {
									echo "<td>" . $value . "</td>";
								}
								echo "</tr>";
								echo "<tr>";
								foreach ($sessionArray as $value) {
									echo "<td>" . $value . "</td>";
								}
								echo "</tr>";

								$counter = 0;
							}
							else
							{
								$counter += 1;
							}
						};?>
					<?php endwhile;?>
					<?php $result->closeCursor();?>
				</table>
			</section>
			<a href="#old-captures"><h2>Anciennes modifications</h2></a>
			<section id="old-captures">
				<?php
				$result->closeCursor();
				$newquery = "SELECT filename, comment, c" . $clientId ."_p" . $projectId . "_screenshots.session FROM `c" . $clientId ."_p" . $projectId . "_screenshots` INNER JOIN `c" . $clientId . "_p" . $projectId . "_comments` ON (c" . $clientId ."_p" . $projectId . "_screenshots.id_surface=c" . $clientId . "_p" . $projectId . "_comments.id_surface) AND  (c" . $clientId ."_p" . $projectId . "_screenshots.session=c" . $clientId . "_p" . $projectId . "_comments.session) WHERE c" . $clientId ."_p" . $projectId . "_screenshots.session!='" . $lastSession->format('Y-m-d H:i:s') . "' ORDER BY c" . $clientId . "_p" . $projectId . "_comments.session DESC;";
				$result = $bdd->query($newquery);
				?>
				<table>
					<?php 
					$counter = 0;
					$totalCount = 0;
					$commentsArray = array();
					$sessionArray = array();
					while ($row = $result->fetch(PDO::FETCH_ASSOC)):
						$thisRowSession = date_create_from_format('Y-m-d H:i:s', $row['session']);
						$thisRowSession->getTimestamp();
						$totalCount += 1;
						if ($thisRowSession != $lastSession) {
							if ($counter == 0) {
								$commentsArray = array();
								$sessionArray = array();
								echo "<tr>";
							}
							array_push($commentsArray, $row['comment']);
							array_push($sessionArray, $row['session']);
							;?>
							<td>
								<div class="imageContainer">
									<img src=<?php echo "http://bimexpo/screenshots/" . $row['filename'] . " alt=\"" . $row['filename'] . "\"";?>  height="200" width="300">
								</div>
							</td>
							<?php
							if ($counter > 3 || $totalCount == $result->rowCount()) {
								echo "</tr>";
								echo "<tr>";
								foreach ($commentsArray as $value) {
									echo "<td>" . $value . "</td>";
								}
								echo "</tr>";
								echo "<tr>";
								foreach ($sessionArray as $value) {
									echo "<td>" . $value . "</td>";
								}
								echo "</tr>";

								$counter = 0;
							}
							else
							{
								$counter += 1;
							}
						};?>
					<?php endwhile;?>
					<?php $result->closeCursor();?>
				</table>
			</section>
		</main>
	</body>
	</html>